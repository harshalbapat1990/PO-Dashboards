<script setup>

import { ref } from 'vue'
import { RouterLink } from 'vue-router';
import { appConfig as config } from '/src/js/config.js'

let props = defineProps( { auth:Object } );

// which tab is last clicked on
let activeTab = ref('home');

const isActive = (menuItem) => activeTab.value === menuItem;
const setActive = (menuItem) => activeTab.value = menuItem;

</script>

<template>
    <div style="background-color: var(--headerBkgColor)" class="mb-3">

        <div class="ms-5 pb-3">
            <header class="d-flex flex-wrap justify-content-center">
                <div class="d-flex align-items-start me-auto text-decoration-none pt-3">
                    <RouterLink to="/"><img alt="logo" src="/images/jacobs.svg" width="125" class="d-inline-block align-top logo" /></RouterLink>
                    <div class="ms-3">
                        <div><span class="h3">App Catalog</span></div>
                    </div>
                </div>
                <ul style="max-height:50px" class="list-group list-group-horizontal">
                    <li class="list-group-item" title="Help">
                        <RouterLink to="/help" class="nav-icon">
                            <i class="bi bi-question-circle-fill"></i>
                        </RouterLink>
                    </li>
                    <li v-if="props.auth.loggedIn" class="list-group-item" title="Account">
                        <RouterLink to="/user" class="nav-icon">
                            <i class="bi bi-person-fill"></i>
                        </RouterLink>
                    </li>
                </ul>
            </header>
        </div>

        <nav class="navbar navbar-expand-lg py-2">
            <div class="container-fluid">
                <div class="collapse navbar-collapse" id="navbarNavDropdown">
                    
                    <ul v-if="props.auth.loggedIn" class="navbar-nav">
                        <li class="nav-item">
                            <RouterLink class="nav-link" aria-current="page" :to="{name:'home'}">All Apps</RouterLink>
                        </li>
                        <li class="nav-item dropdown">
							<a class="nav-link dropdown-toggle" href="#" data-bs-toggle="dropdown" aria-expanded="false">More</a>
							<ul class="dropdown-menu" aria-labelledby="navbarDropdownMenuLink">
								<li>
									<RouterLink class="dropdown-item" :to="{ name: 'pocs' }">Contacts</RouterLink>
								</li>
                                <li>
									<RouterLink class="dropdown-item" :to="{ name: 'repo' }">App Repos</RouterLink>
								</li>
								<li>
									<RouterLink class="dropdown-item" :to="{ name: 'users' }">Users</RouterLink>
								</li>
                                <li>
									<RouterLink class="dropdown-item" :to="{ name: 'docs' }">Documents</RouterLink>
								</li>
							</ul>
						</li>
                        <!-- <li class="nav-item">
                            <RouterLink class="nav-link" aria-current="page" :to="{name:'docs'}">Documents</RouterLink>
                        </li> -->
                        <li class="nav-item">
                            <RouterLink class="nav-link" :to="{ name:'logout' }">Logout</RouterLink>
                        </li>
                    </ul>
                    <!-- Logged out menu -->
                    <ul v-else class="navbar-nav">
                        <li class="nav-item">
                            <RouterLink class="nav-link" aria-current="page" :to="{name:'home'}">Apps</RouterLink>
                        </li>
                        <li class="nav-item">
                            <!-- <RouterLink class="nav-link" style="" aria-current="page" :to="{name:'login'}">Login</RouterLink> -->
                            <a :href="`${config.baseURL}api/login?id=${config.environment}&code=${config.apiKey}`" class="nav-link">Login</a>
                        </li>
                    </ul>

                </div>
            </div>
        </nav>

    </div>

</template>

<style scoped>

.nav-icon i {
  color: #4a4a4a;
  font-size: 1.5rem;
}

a.nav-link {
	color: black;
}

a.nav-link:hover {
	background-color: var(--hoverColor);
	border-radius: 0.3em;
}

.navDown {
	background-color: var(--downColor);
	border-radius: 0.3em;
}

</style>
