<script setup>

import { ref } from 'vue'
import { useRouter } from 'vue-router';
import { AppsModel } from '/src/js/model.js'

let props = defineProps( { auth:Object } );

let model = ref( { pocs:[], waiting:true } );

const repo = new AppsModel();

listPoCs();

/* get all the apps */
function listPoCs() {
	model.value.waiting = true;
	repo.listPoCs().then( json => {
		model.value.waiting = false;
		model.value.pocs = json;
	});
}

/* gets a distinct list of emails only */
function copyEmails(event) {
	const emails = [...new Set( model.value.pocs.map(i => i.poc.toLowerCase()))];
	let body = emails.join(',');
	//console.log(body);
	copyToClipboard(body, 'A list of the distinct emails has been placed on your clipboard.');
}

/* copies a string to the clipboard */
function copyToClipboard(str, msg)
{
    if (!navigator?.clipboard?.writeText)
    {
        alert('The Clipboard API is not available.');
        return;
    }

    navigator.clipboard.writeText( str )
        .then( alert(msg) );
}

</script>

<template>

<div class="container">
	<p class="h3">App Teams</p>

	<div class="mb-3">
		<button type="button" class="btn btn-primary" @click.stop="copyEmails" title="Get an Email List">
			<span class="spinner-border spinner-border-sm" :class="{ 'visually-hidden': !model.waiting }" role="status" aria-hidden="true"></span>
            <i class="bi bi-copy pe-2" :class="{ 'visually-hidden': model.waiting }"></i>
			<span>Copy Distinct Emails</span>
		</button>
	</div>

	<table class="table table-bordered table-hover">
        <thead>
            <tr>
                <th></th>
                <th>App</th>
				<th>PoC</th>
            </tr>
        </thead>
        <tbody>
            <tr v-for="(item,idx) in model.pocs">
                <td><span>{{ idx+1 }}</span></td>
                <td><span><RouterLink :to="{ name: 'update', params:{id:item.appID} }">{{ item.app }}</RouterLink></span></td>
				<td><span>{{ item.poc }}</span></td>
            </tr>
        </tbody>
    </table>

</div>

</template>