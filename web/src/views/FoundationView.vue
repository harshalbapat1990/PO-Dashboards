<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { appConfig } from '/src/js/config.js'
import { getToken, setToken } from '/src/js/storage.js'
import AuthToken from '/src/js/security.js'

const auth = ref(new AuthToken(getToken()))
const busy = ref(false)
const error = ref('')
const notice = ref('')
const products = ref([])
const users = ref([])
const editingProductId = ref('')
const login = reactive({ email: '', password: '' })
const newUser = reactive({ email: '', role: 'User' })
const accessForm = reactive({ resourceType: 'product', resourceId: '', userId: '', role: 'ProductManager' })
const form = reactive({
  organizationName: '', portfolioName: '', productName: '', description: '',
  productType: '', productOwner: '', productManager: '', globalProductLead: '', engineeringManager: '', scrumMaster: '', securityLead: '', status: 'Active', startDate: '', targetDate: '', teamsText: '',
  githubOrganization: '', githubRepositoriesText: '', azureOrganization: '',
  azureProjectsText: '', githubSecretReference: '', azureSecretReference: '',
  syncFrequencyMinutes: 60,
})

const isAdmin = computed(() => auth.value.isAuthorized('admin'))
const apiRoot = appConfig.baseURL || '/'
const portfolioOptions = computed(() => {
  const distinct = new Map()
  for (const product of products.value) {
    if (!distinct.has(product.portfolioId)) distinct.set(product.portfolioId, {
      id: product.portfolioId,
      name: product.portfolioName,
      organizationName: product.organizationName,
    })
  }
  return [...distinct.values()].sort((a, b) => a.organizationName.localeCompare(b.organizationName) || a.name.localeCompare(b.name))
})

function changeAccessScope() {
  accessForm.resourceId = ''
}

function headers(withAuth = true) {
  const values = { 'Content-Type': 'application/json' }
  if (withAuth && auth.value.isValid) values.Authorization = `Bearer ${auth.value.token}`
  return values
}

async function signIn() {
  error.value = ''
  busy.value = true
  try {
    const response = await fetch(`${apiRoot}api/v1/local-auth/token`, {
      method: 'POST', headers: headers(false), body: JSON.stringify(login),
    })
    if (!response.ok) throw new Error(response.status === 401 ? 'Email or local development password was rejected.' : `Local sign-in failed (${response.status}).`)
    const payload = await response.json()
    if (!payload.isSuccess || !payload.result?.token) throw new Error(payload.debug || 'The API did not issue a local admin token.')
    setToken(payload.result.token, payload.result.expires)
    login.password = ''
    window.location.reload()
    return
  } catch (cause) {
    error.value = `${cause.message} Confirm that the local API and Vite proxy are running.`
  } finally {
    busy.value = false
  }
}

async function loadProducts() {
  if (!isAdmin.value) return
  error.value = ''
  busy.value = true
  try {
    const response = await fetch(`${apiRoot}api/v1/foundation/products`, { headers: headers() })
    if (response.status === 401) throw new Error('The admin token was rejected or expired. Sign in again.')
    if (!response.ok) throw new Error(`Could not load products (${response.status}).`)
    const payload = await response.json()
    if (!payload.isSuccess) throw new Error(payload.debug || 'The API could not load product configuration.')
    products.value = payload.result || []
  } catch (cause) {
    error.value = cause.message
  } finally {
    busy.value = false
  }
}

async function loadUsers() {
  const response = await fetch(`${apiRoot}api/v1/foundation/users`, { headers: headers() })
  if (!response.ok) throw new Error(`Could not load users (${response.status}).`)
  const payload = await response.json()
  if (!payload.isSuccess) throw new Error(payload.debug || 'Could not load user assignments.')
  users.value = payload.result || []
}

async function createUser() {
  error.value = ''; notice.value = ''; busy.value = true
  try {
    const response = await fetch(`${apiRoot}api/v1/foundation/users`, { method: 'POST', headers: headers(), body: JSON.stringify(newUser) })
    if (!response.ok) throw new Error(`Could not create user (${response.status}).`)
    const payload = await response.json()
    if (!payload.isSuccess) throw new Error(payload.debug || 'Could not create user.')
    newUser.email = ''; newUser.role = 'User'; await loadUsers(); notice.value = 'User added. Authentication is not configured for invited users yet.'
  } catch (cause) { error.value = cause.message } finally { busy.value = false }
}

async function assignAccess() {
  error.value = ''; notice.value = ''; busy.value = true
  try {
    const path = accessForm.resourceType === 'portfolio' ? 'portfolios' : 'products'
    const response = await fetch(`${apiRoot}api/v1/foundation/${path}/${accessForm.resourceId}/access`, {
      method: 'PUT', headers: headers(), body: JSON.stringify({ userId: accessForm.userId, role: accessForm.role }),
    })
    if (!response.ok) throw new Error(`Could not save access (${response.status}).`)
    notice.value = 'Scoped access saved.'
  } catch (cause) { error.value = cause.message } finally { busy.value = false }
}

function lines(value) {
  return value.split(/\r?\n/).map((line) => line.trim()).filter(Boolean)
}

function parseTeams() {
  return lines(form.teamsText).map((line) => {
    const [name, ...members] = line.split('|')
    return { name: name.trim(), members: (members.join('|').split(',')).map((member) => member.trim()).filter(Boolean) }
  })
}

function parseAzureProjects() {
  return lines(form.azureProjectsText).map((line) => {
    const [id, ...name] = line.split('|')
    return { id: id.trim(), name: name.join('|').trim() }
  })
}

async function createProduct() {
  error.value = ''
  notice.value = ''
  busy.value = true
  try {
    const body = {
      organizationName: form.organizationName,
      portfolioName: form.portfolioName,
      productName: form.productName,
      description: form.description,
      productType: form.productType,
      productOwner: form.productOwner,
      productManager: form.productManager,
      globalProductLead: form.globalProductLead,
      engineeringManager: form.engineeringManager,
      scrumMaster: form.scrumMaster,
      securityLead: form.securityLead,
      status: form.status,
      startDate: form.startDate || null,
      targetDate: form.targetDate || null,
      teams: parseTeams(),
      githubOrganization: form.githubOrganization,
      githubRepositories: lines(form.githubRepositoriesText),
      githubSecretReference: form.githubSecretReference,
      azureOrganization: form.azureOrganization,
      azureProjects: parseAzureProjects(),
      azureSecretReference: form.azureSecretReference,
      syncFrequencyMinutes: Number(form.syncFrequencyMinutes),
    }
    const response = await fetch(`${apiRoot}api/v1/foundation/products${editingProductId.value ? `/${editingProductId.value}` : ''}`, {
      method: editingProductId.value ? 'PUT' : 'POST', headers: headers(), body: JSON.stringify(body),
    })
    if (response.status === 401) throw new Error('The admin token was rejected or expired. Sign in again.')
    if (!response.ok) throw new Error(`Could not save product configuration (${response.status}).`)
    const payload = await response.json()
    if (!payload.isSuccess) throw new Error(payload.debug || 'The API could not save product configuration.')
    notice.value = editingProductId.value ? 'Product and team configuration updated.' : `Product saved with ID ${payload.result}. No GitHub or Azure DevOps connection was made.`
    editingProductId.value = ''
    form.productName = ''
    await loadProducts()
  } catch (cause) {
    error.value = cause.message
  } finally {
    busy.value = false
  }
}

function editProduct(item) {
  editingProductId.value = item.productId
  Object.assign(form, {
    organizationName: item.organizationName, portfolioName: item.portfolioName, productName: item.productName,
    description: item.description, productType: item.productType, productOwner: item.productOwner,
    productManager: item.productManager, globalProductLead: item.globalProductLead,
    engineeringManager: item.engineeringManager, scrumMaster: item.scrumMaster, securityLead: item.securityLead,
    status: item.status, startDate: item.startDate || '', targetDate: item.targetDate || '',
    azureOrganization: item.azureOrganization || '',
    teamsText: (item.teams || []).map((team) => `${team.name} | ${(team.members || []).join(', ')}`).join('\n'),
    azureProjectsText: (item.azureProjects || []).map((project) => `${project.id} | ${project.name}`).join('\n'),
    githubRepositoriesText: (item.githubRepositories || []).join('\n'),
  })
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

onMounted(async () => { if (isAdmin.value) { await loadProducts(); await loadUsers() } })
</script>

<template>
  <main class="container py-4" aria-labelledby="foundation-title">
    <h1 id="foundation-title">Product Health foundation setup</h1>
    <p>Configure the initial organization, portfolio, product, teams, and source references. Saving references does not contact GitHub or Azure DevOps.</p>

    <div v-if="error" class="alert alert-danger" role="alert">{{ error }}</div>
    <div v-if="notice" class="alert alert-success" role="status">{{ notice }}</div>

    <section v-if="!isAdmin" class="card mb-4" aria-labelledby="local-access-title">
      <div class="card-body">
        <h2 id="local-access-title" class="h4">Local development access</h2>
        <p>Use the local admin email and password configured in the API's ignored <code>local.settings.json</code>.</p>
        <form @submit.prevent="signIn">
          <div class="mb-3">
            <label class="form-label" for="local-email">Admin email</label>
            <input id="local-email" v-model.trim="login.email" class="form-control" type="email" autocomplete="username" required>
          </div>
          <div class="mb-3">
            <label class="form-label" for="local-password">Local development password</label>
            <input id="local-password" v-model="login.password" class="form-control" type="password" autocomplete="current-password" required>
          </div>
          <button class="btn btn-primary" type="submit" :disabled="busy">{{ busy ? 'Signing in…' : 'Sign in locally' }}</button>
        </form>
      </div>
    </section>

    <template v-else>
      <section class="card mb-4" aria-labelledby="product-form-title">
        <div class="card-body">
          <h2 id="product-form-title" class="h4">{{ editingProductId ? 'Edit product configuration' : 'Add a product configuration' }}</h2>
          <form @submit.prevent="createProduct">
            <div class="row g-3">
              <div class="col-md-4"><label class="form-label" for="org-name">Organization</label><input id="org-name" v-model.trim="form.organizationName" class="form-control" required maxlength="200"></div>
              <div class="col-md-4"><label class="form-label" for="portfolio-name">Portfolio</label><input id="portfolio-name" v-model.trim="form.portfolioName" class="form-control" required maxlength="200"></div>
              <div class="col-md-4"><label class="form-label" for="product-name">Product</label><input id="product-name" v-model.trim="form.productName" class="form-control" required maxlength="200"></div>
              <div class="col-12"><label class="form-label" for="description">Description</label><textarea id="description" v-model="form.description" class="form-control" rows="2" maxlength="4000"></textarea></div>
              <div class="col-md-4"><label class="form-label" for="product-type">Product type</label><input id="product-type" v-model.trim="form.productType" class="form-control"></div>
              <div class="col-md-4"><label class="form-label" for="owner">Product owner</label><input id="owner" v-model.trim="form.productOwner" class="form-control"></div>
              <div class="col-md-4"><label class="form-label" for="manager">Product manager</label><input id="manager" v-model.trim="form.productManager" class="form-control"></div>
              <div class="col-md-4"><label class="form-label" for="global-lead">Global product lead</label><input id="global-lead" v-model.trim="form.globalProductLead" class="form-control" maxlength="320"></div>
              <div class="col-md-4"><label class="form-label" for="engineering-manager">Engineering manager</label><input id="engineering-manager" v-model.trim="form.engineeringManager" class="form-control" maxlength="320"></div>
              <div class="col-md-4"><label class="form-label" for="scrum-master">Scrum master</label><input id="scrum-master" v-model.trim="form.scrumMaster" class="form-control" maxlength="320"></div>
              <div class="col-md-4"><label class="form-label" for="security-lead">Security lead</label><input id="security-lead" v-model.trim="form.securityLead" class="form-control" maxlength="320"></div>
              <div class="col-md-4"><label class="form-label" for="product-status">Status</label><select id="product-status" v-model="form.status" class="form-select"><option>Active</option><option>Planned</option><option>Paused</option><option>Retired</option></select></div>
              <div class="col-md-4"><label class="form-label" for="start-date">Start date</label><input id="start-date" v-model="form.startDate" class="form-control" type="date"></div>
              <div class="col-md-4"><label class="form-label" for="target-date">Target date</label><input id="target-date" v-model="form.targetDate" class="form-control" type="date"></div>
              <div class="col-md-6"><label class="form-label" for="teams">Teams and members</label><textarea id="teams" v-model="form.teamsText" class="form-control" rows="3" aria-describedby="teams-help"></textarea><div id="teams-help" class="form-text">One per line: Team name | member1@example.test, member2@example.test</div></div>
              <div class="col-md-6"><label class="form-label" for="github-repos">GitHub repository references</label><textarea id="github-repos" v-model="form.githubRepositoriesText" class="form-control" rows="3" aria-describedby="github-help"></textarea><div id="github-help" class="form-text">One repository URL per line. Saved as configuration only.</div></div>
              <div class="col-md-4"><label class="form-label" for="github-org">GitHub organization</label><input id="github-org" v-model.trim="form.githubOrganization" class="form-control"></div>
              <div class="col-md-4"><label class="form-label" for="github-secret">GitHub secret reference</label><input id="github-secret" v-model.trim="form.githubSecretReference" class="form-control" aria-describedby="secret-help"></div>
              <div class="col-md-4"><label class="form-label" for="azure-org">Azure DevOps organization reference</label><input id="azure-org" v-model.trim="form.azureOrganization" class="form-control"></div>
              <div class="col-md-6"><label class="form-label" for="azure-projects">Azure DevOps projects</label><textarea id="azure-projects" v-model="form.azureProjectsText" class="form-control" rows="3" aria-describedby="azure-help"></textarea><div id="azure-help" class="form-text">One per line: project ID | project name. No connection is made.</div></div>
              <div class="col-md-3"><label class="form-label" for="azure-secret">Azure secret reference</label><input id="azure-secret" v-model.trim="form.azureSecretReference" class="form-control" aria-describedby="secret-help"></div>
              <div class="col-md-3"><label class="form-label" for="sync-frequency">Planned sync interval (minutes)</label><input id="sync-frequency" v-model.number="form.syncFrequencyMinutes" class="form-control" type="number" min="1" max="10080" required></div>
              <div id="secret-help" class="col-12 form-text">Enter secret-store reference names only. Never enter access tokens or passwords here.</div>
            </div>
            <button class="btn btn-primary mt-3" type="submit" :disabled="busy">{{ busy ? 'Saving…' : editingProductId ? 'Update product and teams' : 'Save product configuration' }}</button>
            <button v-if="editingProductId" class="btn btn-outline-secondary mt-3 ms-2" type="button" @click="editingProductId = ''; form.productName = ''">Cancel edit</button>
          </form>
        </div>
      </section>

      <section aria-labelledby="configured-products-title">
        <div class="d-flex align-items-center justify-content-between">
          <h2 id="configured-products-title" class="h4">Configured products</h2>
          <button class="btn btn-outline-secondary" type="button" :disabled="busy" @click="loadProducts">Refresh</button>
        </div>
        <p v-if="busy && products.length === 0" role="status">Loading product configuration…</p>
        <p v-else-if="products.length === 0">No product configurations yet.</p>
        <div v-else class="table-responsive">
          <table class="table table-striped align-middle">
            <caption class="visually-hidden">Product configuration records grouped by organization and portfolio</caption>
            <thead><tr><th scope="col">Organization</th><th scope="col">Portfolio</th><th scope="col">Product</th><th scope="col">Status</th><th scope="col">Owner</th><th scope="col">Manager</th><th scope="col">Teams</th><th scope="col">Source references</th><th scope="col">Actions</th></tr></thead>
            <tbody><tr v-for="item in products" :key="item.productId"><td>{{ item.organizationName }}</td><td>{{ item.portfolioName }}</td><td>{{ item.productName }}</td><td>{{ item.status }}</td><td>{{ item.productOwner || '—' }}</td><td>{{ item.productManager || '—' }}</td><td>{{ (item.teams || []).map(team => team.name).join(', ') || '—' }}</td><td>GitHub: {{ item.githubRepositories?.length || 0 }}; Azure DevOps: {{ item.azureProjects?.length || 0 }}</td><td><button class="btn btn-sm btn-outline-primary" type="button" @click="editProduct(item)">Edit</button></td></tr></tbody>
          </table>
        </div>
      </section>

      <section class="card mt-4" aria-labelledby="access-management-title"><div class="card-body">
        <h2 id="access-management-title" class="h4">Users and scoped access</h2>
        <p>Global roles control administrative capabilities. Scoped roles grant access to one product or portfolio. This development setup does not provision login credentials for added users.</p>
        <form class="row g-2 align-items-end mb-3" @submit.prevent="createUser">
          <div class="col-md-6"><label class="form-label" for="new-user-email">User email</label><input id="new-user-email" v-model.trim="newUser.email" class="form-control" type="email" required maxlength="320"></div>
          <div class="col-md-3"><label class="form-label" for="new-user-role">Global role</label><select id="new-user-role" v-model="newUser.role" class="form-select"><option>User</option><option>Admin</option></select></div>
          <div class="col-md-3"><button class="btn btn-outline-primary" type="submit" :disabled="busy">Add user</button></div>
        </form>
        <form class="row g-2 align-items-end" @submit.prevent="assignAccess">
          <div class="col-md-2"><label class="form-label" for="resource-type">Scope</label><select id="resource-type" v-model="accessForm.resourceType" class="form-select" @change="changeAccessScope"><option value="product">Product</option><option value="portfolio">Portfolio</option></select></div>
          <div class="col-md-4"><label class="form-label" for="resource-id">{{ accessForm.resourceType === 'product' ? 'Product' : 'Portfolio' }}</label>
            <select id="resource-id" v-model="accessForm.resourceId" class="form-select" required>
              <option value="" disabled>Select {{ accessForm.resourceType }}</option>
              <template v-if="accessForm.resourceType === 'product'"><option v-for="product in products" :key="product.productId" :value="product.productId">{{ product.productName }} — {{ product.portfolioName }} / {{ product.organizationName }}</option></template>
              <template v-else><option v-for="portfolio in portfolioOptions" :key="portfolio.id" :value="portfolio.id">{{ portfolio.name }} — {{ portfolio.organizationName }}</option></template>
            </select>
            <div v-if="products.length === 0" class="form-text">Create a product hierarchy first.</div>
          </div>
          <div class="col-md-3"><label class="form-label" for="access-user">User</label><select id="access-user" v-model="accessForm.userId" class="form-select" required><option value="" disabled>Select user</option><option v-for="user in users" :key="user.id" :value="user.id">{{ user.email }} ({{ user.role }})</option></select></div>
          <div class="col-md-2"><label class="form-label" for="access-role">Scoped role</label><select id="access-role" v-model="accessForm.role" class="form-select"><option>User</option><option>ProductManager</option><option>PortfolioManager</option></select></div>
          <div class="col-md-1"><button class="btn btn-outline-primary" type="submit" :disabled="busy">Grant</button></div>
        </form>
        <div class="table-responsive mt-3"><table class="table table-sm"><caption class="visually-hidden">Configured platform users and global roles</caption><thead><tr><th>Email</th><th>Global role</th><th>User ID</th></tr></thead><tbody><tr v-for="user in users" :key="user.id"><td>{{ user.email }}</td><td>{{ user.role }}</td><td><code>{{ user.id }}</code></td></tr></tbody></table></div>
      </div></section>
    </template>
  </main>
</template>
