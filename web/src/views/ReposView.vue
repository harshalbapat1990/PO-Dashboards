<script setup>

import { ref } from 'vue'
import { isURL } from '/src/js/utils.js'
import { AppsModel } from '/src/js/model.js'

let props = defineProps( { auth:Object } );

let model = ref( { repos:[], waiting:true } );

const repo = new AppsModel();

listRepos();

/* get all the apps */
function listRepos() {
	model.value.waiting = true;
	repo.listRepos().then( json => {
		model.value.waiting = false;
		model.value.repos = json;
	});
}

</script>

<template>

<div class="container">
	<p class="h3">App Repos</p>

	<!-- <div class="mb-3">
		<button type="button" class="btn btn-primary" @click.stop="copyEmails" title="Get an Email List">
			<span class="spinner-border spinner-border-sm" :class="{ 'visually-hidden': !model.waiting }" role="status" aria-hidden="true"></span>
            <i class="bi bi-copy pe-2" :class="{ 'visually-hidden': model.waiting }"></i>
			<span>Copy Distinct Emails</span>
		</button>
	</div> -->

	<table class="table table-bordered table-hover">
        <thead>
            <tr>
                <!-- <th></th> -->
                <th>App</th>
				<th>Repo URL</th>
            </tr>
        </thead>
        <tbody>
            <tr v-for="(item,idx) in model.repos">
                <!-- <td><span>{{ idx+1 }}</span></td> -->
                <td><span><RouterLink :to="{ name: 'get', params:{id:item.appID} }">{{ item.app }}</RouterLink></span></td>
				<td><span><a :href="item.link">{{ item.link }}</a></span></td>
            </tr>
        </tbody>
    </table>

</div>

</template>