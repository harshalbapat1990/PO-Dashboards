<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { appConfig } from '/src/js/config.js'
import { getToken } from '/src/js/storage.js'
import AuthToken from '/src/js/security.js'

const auth = ref(new AuthToken(getToken()))
const apiRoot = appConfig.baseURL || '/'
const products = ref([])
const selectedProductId = ref('')
const selectedProjectId = ref('')
const selectedType = ref('work_item')
const records = ref([])
const total = ref(0)
const offset = ref(0)
const limit = 100
const busy = ref(false)
const error = ref('')
const notice = ref('')
const fixtureFile = ref(null)
const isAdmin = computed(() => auth.value.isAuthorized('admin'))
const selectedProduct = computed(() => products.value.find(item => item.productId === selectedProductId.value))
const projects = computed(() => selectedProduct.value?.azureProjects || [])
const types = [
  ['project', 'Projects'], ['iteration', 'Iterations / sprints'], ['work_item', 'Work items / bugs'],
  ['pipeline', 'Pipelines'], ['pipeline_run', 'Pipeline runs'], ['test_plan', 'Test plans'],
  ['test_suite', 'Test suites'], ['test_case', 'Test cases'], ['test_run', 'Test runs'], ['test_result', 'Test results'],
]

function headers() {
  const value = { 'Content-Type': 'application/json' }
  if (auth.value.isValid) value.Authorization = `Bearer ${auth.value.token}`
  return value
}

async function loadProducts() {
  const response = await fetch(`${apiRoot}api/v1/foundation/products`, { headers: headers() })
  if (!response.ok) throw new Error(response.status === 401 ? 'Sign in or confirm product access.' : `Could not load products (${response.status}).`)
  const payload = await response.json()
  if (!payload.isSuccess) throw new Error(payload.debug || 'Could not load products.')
  products.value = payload.result || []
  if (!selectedProductId.value && products.value.length) selectedProductId.value = products.value[0].productId
}

async function loadRecords() {
  error.value = ''
  records.value = []
  if (!selectedProductId.value || !selectedProjectId.value) return
  busy.value = true
  try {
    const query = new URLSearchParams({ projectId: selectedProjectId.value, entityType: selectedType.value, limit: String(limit), offset: String(offset.value) })
    const response = await fetch(`${apiRoot}api/v1/foundation/products/${selectedProductId.value}/azure-devops/data?${query}`, { headers: headers() })
    if (!response.ok) throw new Error(response.status === 401 ? 'You do not have access to this product.' : `Could not load stored Azure DevOps data (${response.status}).`)
    const payload = await response.json()
    if (!payload.isSuccess) throw new Error(payload.debug || 'Could not load stored Azure DevOps data.')
    records.value = payload.result?.items || []
    total.value = payload.result?.total || 0
  } catch (cause) { error.value = cause.message } finally { busy.value = false }
}

async function importFixture() {
  error.value = ''; notice.value = ''
  const file = fixtureFile.value?.files?.[0]
  if (!file) { error.value = 'Choose a local normalized snapshot JSON file first.'; return }
  if (!isAdmin.value) { error.value = 'Only a local admin can import snapshot fixtures.'; return }
  busy.value = true
  try {
    const snapshot = JSON.parse(await file.text())
    if (snapshot.projectId !== selectedProjectId.value) throw new Error(`Fixture projectId must match the selected linked project (${selectedProjectId.value}).`)
    const response = await fetch(`${apiRoot}api/v1/foundation/products/${selectedProductId.value}/azure-devops/import`, { method: 'POST', headers: headers(), body: JSON.stringify(snapshot) })
    if (!response.ok) throw new Error(response.status === 401 ? 'Admin authorization was rejected or expired.' : `Import failed (${response.status}).`)
    const payload = await response.json()
    if (!payload.isSuccess) throw new Error(payload.debug || 'Snapshot import failed.')
    const result = payload.result
    notice.value = `Imported ${result.recordsUpserted} records and ${result.relationshipsReceived} relationships; removed ${result.recordsDeleted} records absent from this complete snapshot.`
    await loadRecords()
  } catch (cause) { error.value = cause instanceof SyntaxError ? 'The selected file is not valid JSON.' : cause.message } finally { busy.value = false }
}

function changePage(nextOffset) { offset.value = Math.max(0, nextOffset); void loadRecords() }
function heading(record) {
  const data = record.data || {}
  return data.title || data.name || data.workItemType && `${data.workItemType} ${record.externalId}` || record.externalId
}
function display(value) { return value == null || value === '' ? '—' : typeof value === 'object' ? JSON.stringify(value, null, 2) : String(value) }

watch(selectedProductId, () => { selectedProjectId.value = projects.value[0]?.id || ''; offset.value = 0; void loadRecords() })
watch([selectedProjectId, selectedType], () => { offset.value = 0; void loadRecords() })
onMounted(async () => { busy.value = true; try { await loadProducts() } catch (cause) { error.value = cause.message } finally { busy.value = false } })
</script>

<template>
  <main class="container py-4" aria-labelledby="azure-data-title">
    <h1 id="azure-data-title">Azure DevOps source data</h1>
    <p>Browse normalized records stored in PostgreSQL. Local development never connects to Azure DevOps; load an offline JSON snapshot to exercise the Stage 4 data path.</p>
    <div v-if="error" class="alert alert-danger" role="alert">{{ error }}</div>
    <div v-if="notice" class="alert alert-success" role="status">{{ notice }}</div>
    <section v-if="!auth.isValid" class="alert alert-info" role="status">Sign in through <RouterLink to="/foundation">Foundation</RouterLink> to browse product data.</section>
    <section v-else-if="!products.length && !busy" class="alert alert-info" role="status">No products are available for this account.</section>
    <template v-else>
      <section class="card mb-4" aria-label="Azure DevOps data filters"><div class="card-body row g-3">
        <div class="col-md-5"><label class="form-label" for="ado-product">Product</label><select id="ado-product" v-model="selectedProductId" class="form-select"><option v-for="product in products" :key="product.productId" :value="product.productId">{{ product.productName }}</option></select></div>
        <div class="col-md-4"><label class="form-label" for="ado-project">Linked project</label><select id="ado-project" v-model="selectedProjectId" class="form-select" :disabled="!projects.length"><option v-for="project in projects" :key="project.id" :value="project.id">{{ project.name }} ({{ project.id }})</option></select></div>
        <div class="col-md-3"><label class="form-label" for="ado-type">Data type</label><select id="ado-type" v-model="selectedType" class="form-select"><option v-for="[key, label] in types" :key="key" :value="key">{{ label }}</option></select></div>
      </div></section>
      <section v-if="isAdmin" class="card mb-4" aria-labelledby="fixture-import-title"><div class="card-body">
        <h2 id="fixture-import-title" class="h5">Import local snapshot fixture</h2>
        <p class="mb-2">Import is enabled only in Local API mode. A complete snapshot replaces records and relationships missing from that file for the selected project.</p>
        <div class="d-flex flex-wrap gap-2"><label class="form-label visually-hidden" for="ado-fixture">Snapshot JSON file</label><input id="ado-fixture" ref="fixtureFile" class="form-control" type="file" accept="application/json,.json"><button class="btn btn-primary" type="button" :disabled="busy || !selectedProjectId" @click="importFixture">{{ busy ? 'Working…' : 'Import fixture' }}</button></div>
      </div></section>
      <p v-if="busy" role="status">Loading stored Azure DevOps records…</p>
      <section v-if="selectedProduct && !projects.length" class="alert alert-info" role="status">No Azure DevOps projects are linked to this product. Add a project ID and name in Foundation first.</section>
      <section v-else-if="!records.length && selectedProjectId && !busy" class="alert alert-info" role="status">No {{ types.find(item => item[0] === selectedType)?.[1].toLowerCase() }} have been imported for this project.</section>
      <section v-else class="d-grid gap-3" aria-label="Stored Azure DevOps records">
        <article v-for="record in records" :key="`${record.entityType}:${record.externalId}`" class="card"><div class="card-body">
          <h2 class="h5">{{ heading(record) }}</h2><dl class="record-fields mb-2"><template v-for="(value, key) in record.data" :key="key"><dt>{{ key }}</dt><dd><pre v-if="typeof value === 'object' && value !== null">{{ display(value) }}</pre><span v-else>{{ display(value) }}</span></dd></template></dl>
          <small class="text-body-secondary">{{ record.entityType }} · ID {{ record.externalId }} · imported {{ new Date(record.syncedAt).toLocaleString() }}</small>
        </div></article>
      </section>
      <nav v-if="total > limit" class="d-flex align-items-center justify-content-between mt-3" aria-label="Azure DevOps records pages"><button class="btn btn-outline-secondary" type="button" :disabled="busy || offset === 0" @click="changePage(offset - limit)">Previous</button><span aria-live="polite">Showing {{ offset + 1 }}–{{ Math.min(offset + records.length, total) }} of {{ total }}</span><button class="btn btn-outline-secondary" type="button" :disabled="busy || offset + limit >= total" @click="changePage(offset + limit)">Next</button></nav>
    </template>
  </main>
</template>

<style scoped>
.record-fields { display: grid; grid-template-columns: minmax(8rem, 1fr) minmax(0, 3fr); gap: .4rem 1rem; }
.record-fields dt, .record-fields dd { overflow-wrap: anywhere; }
.record-fields dd { margin: 0; }
.record-fields pre { margin: 0; white-space: pre-wrap; font: inherit; }
</style>
