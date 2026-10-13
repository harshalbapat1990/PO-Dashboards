<script setup>

import { ref } from 'vue'
import { isEmpty, isURL, dateAsLocal } from '/src/js/utils.js'
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
	repo.listReadmes().then( json => {
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

</script>

<template>

<div class="container">

    <div class="mb-3">
        <button type="button" class="btn btn-primary" @click.stop="listApps()">
            <span class="spinner-border spinner-border-sm" :class="{ 'visually-hidden': !model.waiting }" role="status" aria-hidden="true"></span>
			<span :class="{ 'visually-hidden': model.waiting }">Refresh</span>
        </button>
    </div>
    
    <table class="table table-bordered table-hover">
        <thead>
            <tr>
                <th></th>
                <th>%</th>
                <th>Repo</th>
                <th>Details</th>
                <th>Is Prod</th>
                <th>Site</th>
                <th>PoC</th>
                <th>Prod Branch</th>
                <th>Modified</th>
            </tr>
        </thead>
        <tbody>
            <tr v-for="(item,idx) in model.apps">
                <template v-if="item.error">
                    <td><span>{{ idx+1 }}</span></td>
                    <td colspan="8"><span>Parse Error on</span>&nbsp;<a :href="item.url">{{ item.name }}</a></td>
                </template>
                <template v-else>
                    <td><span>{{ idx+1 }}</span></td>
                    <td><span>{{ `${item.score}%` }}</span></td>
                    <td class="actionIcon"><i v-if="isURL(item.url)" class="bi bi-git" @click.stop="doOpenURL(item.url)" title="Go to Repo" /><span v-else>&nbsp;</span></td>
                    <td>
                        <span><RouterLink :to="{ name: 'get', params:{id:item.id} }">{{ item.name }}</RouterLink></span>
                    </td>
                    <td class="actionIcon"><i v-if="item.isProd" class="bi bi-check-square-fill" title="In Production" /></td>
                    <td class="actionIcon"><i v-if="isURL(item.website.url)" class="bi bi-globe" @click.stop="doOpenURL(item.website.url)" title="Go to Site" /><span v-else>&nbsp;</span></td>
                    <td><span>{{ item.team[0]?.name }}</span></td>
                    <td><span>{{ item.prodBranch }}</span></td>
                    <td><span v-html="dateAsLocal(item.ts)"></span></td>
                </template>
            </tr>
        </tbody>
    </table>

</div>

</template>

<style scoped>
.actionIcon {
	text-align: center;
    cursor: pointer;

}
</style>
