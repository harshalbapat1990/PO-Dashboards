<script setup>
import { computed, onMounted, ref } from 'vue'
import { appConfig } from '/src/js/config.js'
import { getToken } from '/src/js/storage.js'
import AuthToken from '/src/js/security.js'

const auth = ref(new AuthToken(getToken()))
const apiRoot = appConfig.baseURL || '/'
const products = ref([])
const selectedProductId = ref('')
const configuration = ref(null)
const busy = ref(false)
const error = ref('')
const notice = ref('')
const tokenIsValid = computed(() => auth.value.isValid)
const orderedModules = computed(() => {
  if (!configuration.value) return []
  const byKey = new Map(configuration.value.modules.map((module) => [module.key, module]))
  return configuration.value.moduleOrder.map((key) => byKey.get(key)).filter(Boolean)
})
const visibleModules = computed(() => orderedModules.value.filter((module) =>
  configuration.value.enabledModules.includes(module.key)
  && configuration.value.widgetConfiguration[module.key]?.visible,
))

function headers() {
  const values = { 'Content-Type': 'application/json' }
  if (auth.value.isValid) values.Authorization = `Bearer ${auth.value.token}`
  return values
}

async function loadProducts() {
  error.value = ''
  if (!tokenIsValid.value) return
  const response = await fetch(`${apiRoot}api/v1/foundation/products`, { headers: headers() })
  if (!response.ok) throw new Error(response.status === 401 ? 'Sign in or ask an administrator to grant product access.' : `Could not load products (${response.status}).`)
  const payload = await response.json()
  if (!payload.isSuccess) throw new Error(payload.debug || 'Could not load products.')
  products.value = payload.result || []
  if (!selectedProductId.value && products.value.length) selectedProductId.value = products.value[0].productId
  if (selectedProductId.value && products.value.some((p) => p.productId === selectedProductId.value)) {
    await loadConfiguration(selectedProductId.value)
  }
}

async function loadConfiguration(productId) {
  if (!productId) { configuration.value = null; return }
  error.value = ''; notice.value = ''; busy.value = true
  try {
    const response = await fetch(`${apiRoot}api/v1/foundation/products/${productId}/dashboard-configuration`, { headers: headers() })
    if (!response.ok) throw new Error(response.status === 401 ? 'You do not have access to configure or view this product dashboard.' : `Could not load dashboard configuration (${response.status}).`)
    const payload = await response.json()
    if (!payload.isSuccess) throw new Error(payload.debug || 'Could not load dashboard configuration.')
    configuration.value = payload.result
  } catch (cause) {
    error.value = cause.message
    configuration.value = null
  } finally { busy.value = false }
}

function toggleModule(key, enabled) {
  const current = configuration.value.enabledModules
  configuration.value.enabledModules = enabled ? [...new Set([...current, key])] : current.filter((item) => item !== key)
}

function moveModule(key, direction) {
  const order = [...configuration.value.moduleOrder]
  const index = order.indexOf(key)
  const next = index + direction
  if (next < 0 || next >= order.length) return
  ;[order[index], order[next]] = [order[next], order[index]]
  configuration.value.moduleOrder = order
}

function restoreDefaults() {
  if (!configuration.value) return
  configuration.value.enabledModules = configuration.value.modules.filter((module) => module.defaultEnabled).map((module) => module.key)
  configuration.value.moduleOrder = configuration.value.modules.map((module) => module.key)
  configuration.value.widgetConfiguration = Object.fromEntries(configuration.value.modules.map((module) => [module.key, { visible: true, size: 'medium' }]))
  configuration.value.dashboardLayout = { columns: 2 }
  configuration.value.defaultFilters = { owner: '', status: '', team: '' }
  configuration.value.defaultDateRange = { preset: '30d', startDate: null, endDate: null }
}

async function restoreAndSaveDefaults() {
  restoreDefaults()
  await saveConfiguration('Default layout restored.')
}

async function saveConfiguration(successMessage = 'Dashboard configuration saved.') {
  if (!configuration.value) return
  error.value = ''; notice.value = ''; busy.value = true
  try {
    const response = await fetch(`${apiRoot}api/v1/foundation/products/${selectedProductId.value}/dashboard-configuration`, {
      method: 'PUT', headers: headers(), body: JSON.stringify(configuration.value),
    })
    if (response.status === 401) throw new Error('Only an Admin or a product/portfolio manager can save this dashboard.')
    if (response.status === 400) throw new Error(await response.text() || 'The configuration contains unsupported values.')
    if (!response.ok) throw new Error(`Could not save dashboard configuration (${response.status}).`)
    const payload = await response.json()
    if (!payload.isSuccess) throw new Error(payload.debug || 'Could not save dashboard configuration.')
    configuration.value = payload.result
    notice.value = successMessage
  } catch (cause) { error.value = cause.message } finally { busy.value = false }
}

function sizeSpan(size) {
  const spans = { small: 1, medium: 2, large: 3 }
  return Math.min(spans[size] || 2, configuration.value?.dashboardLayout?.columns || 2)
}

onMounted(async () => {
  if (!tokenIsValid.value) return
  busy.value = true
  try { await loadProducts() } catch (cause) { error.value = cause.message } finally { busy.value = false }
})
</script>

<template>
  <main class="container py-4" aria-labelledby="dashboard-title">
    <h1 id="dashboard-title">Product dashboard</h1>
    <p>Configure the modules and layout for a product. Dashboard cards are placeholders until later data stages provide metrics.</p>
    <div v-if="error" class="alert alert-danger" role="alert">{{ error }}</div>
    <div v-if="notice" class="alert alert-success" role="status">{{ notice }}</div>

    <section v-if="!tokenIsValid" class="alert alert-info" aria-live="polite">
      Sign in through <RouterLink to="/foundation">Foundation</RouterLink> to view dashboards. A product or portfolio administrator must grant access.
    </section>
    <section v-else-if="products.length === 0 && !busy" class="alert alert-info" role="status">
      No products are available for this account. <RouterLink to="/foundation">Configure a product</RouterLink> or ask an administrator to grant access.
    </section>
    <template v-else-if="configuration">
      <section class="card mb-4" aria-labelledby="dashboard-settings-title">
        <div class="card-body">
          <h2 id="dashboard-settings-title" class="h4">Dashboard configuration</h2>
          <div class="row g-3 align-items-end">
            <div class="col-md-6">
              <label class="form-label" for="dashboard-product">Product</label>
              <select id="dashboard-product" v-model="selectedProductId" class="form-select" :disabled="busy" @change="loadConfiguration(selectedProductId)">
                <option v-for="product in products" :key="product.productId" :value="product.productId">{{ product.productName }} — {{ product.portfolioName }}</option>
              </select>
            </div>
            <div class="col-md-3">
              <label class="form-label" for="dashboard-columns">Desktop columns</label>
              <select id="dashboard-columns" v-model.number="configuration.dashboardLayout.columns" class="form-select"><option :value="1">1 column</option><option :value="2">2 columns</option><option :value="3">3 columns</option></select>
            </div>
            <div class="col-md-3 d-flex gap-2">
              <button class="btn btn-primary" type="button" :disabled="busy" @click="saveConfiguration()">{{ busy ? 'Saving…' : 'Save configuration' }}</button>
              <button class="btn btn-outline-secondary" type="button" :disabled="busy" @click="restoreAndSaveDefaults">Restore defaults</button>
            </div>
          </div>

          <div class="row g-3 mt-2">
            <div class="col-md-4"><label class="form-label" for="filter-owner">Default owner filter</label><input id="filter-owner" v-model="configuration.defaultFilters.owner" class="form-control" maxlength="200"></div>
            <div class="col-md-4"><label class="form-label" for="filter-status">Default status filter</label><input id="filter-status" v-model="configuration.defaultFilters.status" class="form-control" maxlength="200"></div>
            <div class="col-md-4"><label class="form-label" for="filter-team">Default team filter</label><input id="filter-team" v-model="configuration.defaultFilters.team" class="form-control" maxlength="200"></div>
            <div class="col-md-4"><label class="form-label" for="default-range">Default date range</label><select id="default-range" v-model="configuration.defaultDateRange.preset" class="form-select"><option value="7d">Last 7 days</option><option value="30d">Last 30 days</option><option value="90d">Last 90 days</option><option value="custom">Custom dates</option></select></div>
            <template v-if="configuration.defaultDateRange.preset === 'custom'">
              <div class="col-md-4"><label class="form-label" for="range-start">Start date</label><input id="range-start" v-model="configuration.defaultDateRange.startDate" class="form-control" type="date" required></div>
              <div class="col-md-4"><label class="form-label" for="range-end">End date</label><input id="range-end" v-model="configuration.defaultDateRange.endDate" class="form-control" type="date" required></div>
            </template>
          </div>

          <h3 class="h5 mt-4">Modules</h3>
          <p class="form-text">Enable a module, control its visibility, choose its card size, and move it in the dashboard order.</p>
          <div class="table-responsive">
            <table class="table align-middle">
              <caption class="visually-hidden">Module enablement, visibility, card size, and order controls</caption>
              <thead><tr><th scope="col">Module</th><th scope="col">Enabled</th><th scope="col">Visible</th><th scope="col">Card size</th><th scope="col">Order</th></tr></thead>
              <tbody>
                <tr v-for="(module, index) in orderedModules" :key="module.key">
                  <th scope="row">{{ module.name }}</th>
                  <td><input class="form-check-input" type="checkbox" :aria-label="`Enable ${module.name}`" :checked="configuration.enabledModules.includes(module.key)" @change="toggleModule(module.key, $event.target.checked)"></td>
                  <td><input class="form-check-input" type="checkbox" :aria-label="`Show ${module.name}`" v-model="configuration.widgetConfiguration[module.key].visible"></td>
                  <td><select v-model="configuration.widgetConfiguration[module.key].size" class="form-select"><option value="small">Small</option><option value="medium">Medium</option><option value="large">Large</option></select></td>
                  <td><div class="btn-group btn-group-sm" role="group" :aria-label="`Move ${module.name}`"><button class="btn btn-outline-secondary" type="button" :disabled="index === 0" :aria-label="`Move ${module.name} up`" @click="moveModule(module.key, -1)">↑</button><button class="btn btn-outline-secondary" type="button" :disabled="index === orderedModules.length - 1" :aria-label="`Move ${module.name} down`" @click="moveModule(module.key, 1)">↓</button></div></td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </section>

      <section aria-labelledby="dashboard-preview-title">
        <h2 id="dashboard-preview-title" class="h4">{{ products.find((p) => p.productId === selectedProductId)?.productName }} dashboard preview</h2>
        <p v-if="visibleModules.length === 0" class="text-body-secondary">No dashboard modules are enabled and visible. Enable a module above to add it to the preview.</p>
        <div v-else class="dashboard-module-grid" :style="{ '--dashboard-columns': configuration.dashboardLayout.columns }">
          <article v-for="module in visibleModules" :key="module.key" class="card h-100" :style="{ '--module-span': sizeSpan(configuration.widgetConfiguration[module.key].size) }">
            <div class="card-body"><h3 class="h5">{{ module.name }}</h3><p class="mb-0 text-body-secondary">Configuration is ready. Metrics will be added in later delivery stages.</p><span class="badge text-bg-light mt-3">{{ configuration.widgetConfiguration[module.key].size }}</span></div>
          </article>
        </div>
      </section>
    </template>
    <p v-else-if="busy" role="status">Loading product dashboard configuration…</p>
  </main>
</template>

<style scoped>
.dashboard-module-grid { display: grid; grid-template-columns: repeat(var(--dashboard-columns), minmax(0, 1fr)); gap: 1rem; }
.dashboard-module-grid > * { grid-column: span var(--module-span); }
@media (max-width: 767.98px) { .dashboard-module-grid { grid-template-columns: minmax(0, 1fr); } .dashboard-module-grid > * { grid-column: 1; } }
</style>
