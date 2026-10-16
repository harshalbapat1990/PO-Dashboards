<template>
  <header class="border-bottom bg-body">
    <div class="container py-3">
      <a class="h4 mb-0 text-decoration-none text-body" href="/foundation">
        Product Health Dashboard
      </a>
      <nav class="d-flex gap-3 mt-2" aria-label="Main navigation">
        <RouterLink to="/dashboard">Dashboard</RouterLink>
        <RouterLink to="/github-data">GitHub data</RouterLink>
        <RouterLink to="/azure-devops-data">Azure DevOps data</RouterLink>
        <RouterLink to="/foundation">Foundation</RouterLink>
      </nav>
    </div>
  </header>
</template>
