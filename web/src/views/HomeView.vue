<script setup>

import { ref } from 'vue'
import { isURL } from '/src/js/utils.js'
import { AppsModel } from '/src/js/model.js'
import { useRouter } from 'vue-router';

const router = useRouter();

let props = defineProps( { auth:Object } );

let model = ref( { apps:[], waiting: false } );
const isAdmin = props.auth.isAuthorized('admin');

const repo = new AppsModel();
listApps();

/* get all the apps */
function listApps() {
	model.value.waiting = true;
	repo.listApps().then( json => {
		model.value.waiting = false;
		model.value.apps = json;
	});
}

/* go to the edit view */
function doEdit( event, item ) {
    router.push( { name: 'update', params:{ id: item.id } } );
}

/* go to the read view */
function doRead( event, item ) {
    router.push( { name: 'get', params:{ id: item.id } } );
}

/* go to the read view */
function doOpenURL( url ) {
    window.open(url);
}

/* create a json data file and force the download */
async function doDownload(event, item)
{
    repo.getApp(item.id).then( json => {
        if ( !json ) return;

        const blob = new Blob([JSON.stringify(json, null, 2)], { type: 'text/json;charset=utf-8;' });
        const fileURL = URL.createObjectURL(blob);
        const downloadLink = document.createElement('a');
        downloadLink.href = fileURL;
        downloadLink.download = `${item.name}.json`;
        document.body.appendChild(downloadLink);
        downloadLink.click();
        URL.revokeObjectURL(fileURL);
        document.body.removeChild(downloadLink);
    });
}

/* when the delete Button is clicked */
function doDelete(id)
{
    const app = model.value.apps.find(app => app.id === id );

    if (!app) return;

    const msg = `Are you super certain you want to delete the app '${app.name}'?\nDownload it's JSON first if you are not certain.`;

    if ( !isAdmin || !confirm(msg) ) return;

    repo.deleteApp(id).then( json => {
        if ( !json ) return;
        //alert('Deleted');
        listApps();
    })
}

</script>

<template>

<div class="container">
    <!-- <p class="h3">App Catalog</p> -->

    <div class="mb-3">
        <button type="button" class="btn btn-primary" @click.stop="router.push( { name: 'insert' } )">
            <span class="spinner-border spinner-border-sm" :class="{ 'visually-hidden': !model.waiting }" role="status" aria-hidden="true"></span>
			<span :class="{ 'visually-hidden': model.waiting }">Add New App</span>
        </button>
    </div>

    <table class="table table-bordered table-hover">
        <thead>
            <tr>
                <th></th>
                <th>Edit</th>
                <th>App</th>
                <th>%</th>
                <th>Site</th>
                <th>Repository</th>
                <th>Prod Branch</th>
                <th>Download</th>
                <th>Review</th>
                <th v-if="isAdmin">Delete</th>
            </tr>
        </thead>
        <tbody>
            <tr v-for="(item,idx) in model.apps">
                <td><span>{{ idx+1 }}</span></td>
                <td class="actionIcon"><i class="bi bi-pencil-fill" @click.stop="(evt) => doEdit(evt, item)" title="Edit Record" /></td>
                <td><span><RouterLink :to="{ name: 'getold', params:{id:item.id} }">{{ item.name }}</RouterLink></span></td>
                <td><span>{{ `${item.score}%` }}</span></td>
                <td class="actionIcon"><i v-if="isURL(item.webSite)" class="bi bi-globe" @click.stop="doOpenURL(item.webSite)" title="Go to Site" /><span v-else>&nbsp;</span></td>
                <td class="actionIcon"><i v-if="isURL(item.repo)" class="bi bi-git" @click.stop="doOpenURL(item.repo)" title="Go to Repo" /><span v-else>&nbsp;</span></td>
                <!-- <td><span><a :href="item.repo" target="_blank">{{ item.repo }}</a></span></td> -->
                <td><span>{{ item.branch }}</span></td>
                <td class="actionIcon"><i class="bi bi-cloud-download-fill" @click.stop="(evt) => doDownload(evt, item)" title="Download Entry Data" /></td>
                <td class="actionIcon">
                    <i v-if="isURL(item.review)" class="bi bi-card-checklist" @click.stop="doOpenURL(item.review)" title="Go to Review" /><span v-else>&nbsp;</span>
                </td>
                <td v-if="isAdmin" class="actionIcon"><i class="bi bi-trash-fill" @click="(evt) => doDelete(item.id)"></i></td>
            </tr>
        </tbody>
    </table>

</div>

</template>

<style scoped>
.actionIcon {
	text-align: center;

}
</style>
