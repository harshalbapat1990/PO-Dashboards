<script setup>

import { ref } from 'vue'
import { AppsModel } from '/src/js/model.js'

let props = defineProps( { auth:Object } );

let model = ref( { users:[], waiting:true } );

const repo = new AppsModel();

listUsers();

/* get all the apps */
function listUsers() {
	model.value.waiting = true;
	repo.listUsers().then( json => {
		model.value.waiting = false;
		model.value.users = json;
	});
}

</script>

<template>

<div class="container">
	<p class="h3">Users</p>

    <div class="mb-3">
        <button type="button" class="btn btn-primary" disabled :class="{ 'visually-hidden': !model.waiting }">
            <span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>
            <span>Loading...</span>
        </button>
    </div>

	<table class="table table-bordered table-hover">
        <thead>
            <tr>
                <th></th>
                <th>User</th>
            </tr>
        </thead>
        <tbody>
            <tr v-for="(item,idx) in model.users">
                <td><span>{{ idx+1 }}</span></td>
				<td><span>{{ item.email }}</span></td>
            </tr>
        </tbody>
    </table>

</div>

</template>