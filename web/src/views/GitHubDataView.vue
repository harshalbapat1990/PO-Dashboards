<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { appConfig } from '/src/js/config.js'
import { getToken } from '/src/js/storage.js'
import AuthToken from '/src/js/security.js'

const auth = ref(new AuthToken(getToken()))
const apiRoot = appConfig.baseURL || '/'
const products = ref([])
const selectedProductId = ref('')
const selectedRepository = ref('')
const selectedType = ref('repository')
const records = ref([])
const totalRecords = ref(0)
const offset = ref(0)
const pageSize = 100
const busy = ref(false)
const error = ref('')
const types = [
  { key: 'repository', label: 'Repository' },
  { key: 'branch', label: 'Branches' },
  { key: 'commit', label: 'Commits' },
  { key: 'pull_request', label: 'Pull requests' },
  { key: 'release', label: 'Releases' },
  { key: 'workflow', label: 'Actions workflows' },
  { key: 'workflow_run', label: 'Actions runs' },
  { key: 'security_alert', label: 'Security alerts' },
]
const selectedProduct = computed(() => products.value.find((product) => product.productId === selectedProductId.value))
const repositories = computed(() => selectedProduct.value?.githubRepositories || [])

function headers() {
  const values = { 'Content-Type': 'application/json' }
  if (auth.value.isValid) values.Authorization = `Bearer ${auth.value.token}`
  return values
}

async function loadProducts() {
  if (!auth.value.isValid) return
  const response = await fetch(`${apiRoot}api/v1/foundation/products`, { headers: headers() })
  if (!response.ok) throw new Error(response.status === 401 ? 'Sign in to view GitHub data.' : `Could not load products (${response.status}).`)
  const payload = await response.json()
  if (!payload.isSuccess) throw new Error(payload.debug || 'Could not load products.')
  products.value = payload.result || []
  if (!selectedProductId.value && products.value.length) selectedProductId.value = products.value[0].productId
}

async function loadRecords() {
  error.value = ''
  records.value = []
  if (!selectedProductId.value || !selectedRepository.value) return
  busy.value = true
  try {
    const query = new URLSearchParams({ repositoryUrl: selectedRepository.value, entityType: selectedType.value, limit: String(pageSize), offset: String(offset.value) })
    const response = await fetch(`${apiRoot}api/v1/foundation/products/${selectedProductId.value}/github/data?${query}`, { headers: headers() })
    if (!response.ok) throw new Error(response.status === 401 ? 'You do not have access to this product.' : `Could not load GitHub data (${response.status}).`)
    const payload = await response.json()
    if (!payload.isSuccess) throw new Error(payload.debug || 'Could not load GitHub data.')
    records.value = payload.result?.items || []
    totalRecords.value = payload.result?.total || 0
  } catch (cause) { error.value = cause.message } finally { busy.value = false }
}

function changePage(nextOffset) {
  offset.value = Math.max(0, Math.min(nextOffset, Math.max(totalRecords.value - 1, 0)))
  void loadRecords()
}

function isStale(record) {
  return Date.now() - new Date(record.syncedAt).getTime() > 24 * 60 * 60 * 1000
}

function displayValue(value) {
  if (value === null || value === undefined || value === '') return '—'
  return typeof value === 'object' ? JSON.stringify(value, null, 2) : String(value)
}

function heading(record) {
  const data = record.data || {}
  return data.fullName || data.title || data.name || data.tag || data.commitId || data.alertId || record.externalId
}

watch(selectedProductId, () => {
  selectedRepository.value = repositories.value[0] || ''
  void loadRecords()
})
watch([selectedRepository, selectedType], () => { offset.value = 0; void loadRecords() })

onMounted(async () => {
  busy.value = true
  try { await loadProducts() } catch (cause) { error.value = cause.message } finally { busy.value = false }
})
</script>

<template>
  <main class="container py-4" aria-labelledby="github-title">
    <h1 id="github-title">GitHub source data</h1>
    <p>Browse GitHub snapshots stored in PostgreSQL. Local development does not connect to GitHub; use the local snapshot import API to load data.</p>
    <div v-if="error" class="alert alert-danger" role="alert">{{ error }}</div>
    <section v-if="!auth.isValid" class="alert alert-info" role="status">Sign in through <RouterLink to="/foundation">Foundation</RouterLink> to view product data.</section>
    <section v-else-if="products.length === 0 && !busy" class="alert alert-info" role="status">No products are available for this account.</section>
    <section v-else class="card mb-4" aria-label="GitHub data filters">
      <div class="card-body row g-3">
        <div class="col-md-5">
          <label class="form-label" for="github-product">Product</label>
          <select id="github-product" v-model="selectedProductId" class="form-select">
            <option v-for="product in products" :key="product.productId" :value="product.productId">{{ product.productName }}</option>
          </select>
        </div>
        <div class="col-md-5">
          <label class="form-label" for="github-repository">Repository</label>
          <select id="github-repository" v-model="selectedRepository" class="form-select" :disabled="repositories.length === 0">
            <option v-for="repository in repositories" :key="repository" :value="repository">{{ repository }}</option>
          </select>
        </div>
        <div class="col-md-2">
          <label class="form-label" for="github-type">Data type</label>
          <select id="github-type" v-model="selectedType" class="form-select">
            <option v-for="type in types" :key="type.key" :value="type.key">{{ type.label }}</option>
          </select>
        </div>
      </div>
    </section>
    <p v-if="busy" role="status">Loading stored GitHub records…</p>
    <div v-if="records.some(isStale)" class="alert alert-warning" role="status">Some displayed records were imported more than 24 hours ago. Treat this snapshot as stale.</div>
    <section v-else-if="selectedProduct && repositories.length === 0" class="alert alert-info" role="status">This product has no linked GitHub repositories.</section>
    <section v-else-if="records.length === 0 && selectedRepository" class="alert alert-info" role="status">No {{ types.find((type) => type.key === selectedType)?.label.toLowerCase() }} have been imported for this repository.</section>
    <section v-else class="d-grid gap-3" aria-label="Stored GitHub records">
      <article v-for="record in records" :key="`${record.entityType}:${record.externalId}`" class="card">
        <div class="card-body">
          <h2 class="h5">{{ heading(record) }}</h2>
          <span v-if="isStale(record)" class="badge text-bg-warning mb-2">Stale snapshot</span>
          <dl class="github-fields mb-0">
            <template v-for="(value, key) in record.data" :key="key">
              <dt>{{ key }}</dt><dd><pre v-if="typeof value === 'object' && value !== null">{{ displayValue(value) }}</pre><span v-else>{{ displayValue(value) }}</span></dd>
            </template>
          </dl>
          <small class="text-body-secondary">Imported {{ new Date(record.syncedAt).toLocaleString() }}</small>
        </div>
      </article>
    </section>
    <nav v-if="totalRecords > pageSize" class="d-flex align-items-center justify-content-between mt-3" aria-label="GitHub records pages">
      <button class="btn btn-outline-secondary" type="button" :disabled="busy || offset === 0" @click="changePage(offset - pageSize)">Previous</button>
      <span aria-live="polite">Showing {{ offset + 1 }}–{{ Math.min(offset + records.length, totalRecords) }} of {{ totalRecords }}</span>
      <button class="btn btn-outline-secondary" type="button" :disabled="busy || offset + pageSize >= totalRecords" @click="changePage(offset + pageSize)">Next</button>
    </nav>
  </main>
</template>

<style scoped>
.github-fields { display: grid; grid-template-columns: minmax(8rem, 1fr) minmax(0, 3fr); gap: .4rem 1rem; }
.github-fields dt { overflow-wrap: anywhere; }
.github-fields dd { margin: 0; overflow-wrap: anywhere; }
.github-fields pre { margin: 0; white-space: pre-wrap; font: inherit; }
</style>
