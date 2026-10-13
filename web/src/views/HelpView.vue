<script setup>

import { appConfig as config } from '/src/js/config.js'

let props = defineProps( { auth:Object } );

</script>

<template>

<div class="container">
	<p class="h3">Help</p>

	<p>App Catalog collects metadata on DA Apps. Since it uses a static web app + JSON REST architecture - you can fetch the data from the API directly with an integration token.</p>

	<p v-if="!props.auth.loggedIn">
		You must first authenticate with Ping by <a :href="`${config.baseURL}api/login?id=${config.environment}&code=${config.apiKey}`">Logging In</a>.
	</p>
	
	<div>
		<ul>
			<li>Click 'Add New App' to create a new entry.</li>
			<li>Click the row edit icon to modify an existing entry.</li>
			<li>Click the row view icon or the name itself to view an existing entry.</li>
			<li>The repo column is a link to the app's primary Git Repo. You may not have access.</li>
		</ul>
	</div>

	<p>Contact <span class="fw-bold">tag.guillory@jacobs.com</span> for assistance.</p>

</div>

</template>