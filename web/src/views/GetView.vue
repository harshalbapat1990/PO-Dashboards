<script setup>

import { onMounted, ref } from 'vue'
import { appConfig as config } from '/src/js/config.js'
import { isEmpty, truncate, dateAsLocal } from '/src/js/utils.js'
import { useRoute, useRouter, onBeforeRouteUpdate } from 'vue-router';
import { AppsModel } from '/src/js/model.js'

const router = useRouter();
const route = useRoute();

let model = ref( { app:{ name:'App', isProd:false }, waiting: false, page: {} } );
model.value.app.id = route.params.id;

const repo = new AppsModel();

/* when the view is mounted */
onMounted( async () => {
	getApp();
});

/* watches the route for params change */
onBeforeRouteUpdate( async (to, from) => {
	if (to.params.id == from.params.id) return;
	model.value.app.id = to.params.id;
	getApp();
});

/* get or reload an app */
function getApp() {
	model.value.waiting = true;
	repo.getApp(model.value.app.id)
	.then( json => {
		model.value.waiting = false;
		// reformat fields here instead of on each bind
		model.value.app = json;
		document.title = `${config.baseTitle}-${model.value.app.name}`;
	});

	// get the next page info
	repo.getPage(model.value.app.id).then( json => { model.value.page = json; } );
}

/* replaces all newlines with a br tag */
const formatP = (str) => isEmpty(str) ? '' : str.replaceAll('\n', '<br/>');

</script>

<template>

<div class="container">
	<p class="h3"><span>{{ model.app.name }}</span><span class="ms-2">[{{ model.app.isProd ? 'Production' : 'Non-Production' }}]</span></p>

	<div class="btn-group mb-3" role="group">
		<button type="button" class="btn btn-secondary" @click.stop="router.push( { name: 'get', params:{id:model.page.prev} } )" title="Previous">
			<i class="bi bi-arrow-left-circle-fill"></i>
		</button>
		<button type="button" class="btn btn-primary" @click.stop="router.push( { name: 'home' } )" title="All">
			<i class="bi bi-arrow-up-circle-fill"></i>
		</button>
		
		<button type="button" class="btn btn-secondary" @click.stop="router.push( { name: 'get', params:{id:model.page.next} } )" title="Next">
			<i class="bi bi-arrow-right-circle-fill"></i>
		</button>
		<button type="button" class="btn btn-primary" @click.stop="router.push( { name: 'update', params:{id:model.app.id} } )">
			<span class="spinner-border spinner-border-sm" :class="{ 'visually-hidden': !model.waiting }" role="status" aria-hidden="true"></span>
			<span :class="{ 'visually-hidden': model.waiting }">Edit</span>
		</button>
	</div>

	<div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Aliases</label>
  			<label class=" col-10 form-label" v-html="model.app.aliases?.join(', ')"></label>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Team</label>
			<div class="col-10">
				<div v-for="item in model.app.team"><span v-if="model.app.team.length > 1">&bull;</span>{{ item }}</div>
			</div>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Description</label>
  			<label class="col-10 form-label" v-html="formatP(model.app.description)"></label>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Repos</label>
  			<div class="col-10">
				<div v-for="item in model.app.repos"><span v-if="model.app.repos.length > 1">&bull;</span><a :href="item" target="_blank">{{ item }}</a></div>
			</div>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Prod Branch</label>
  			<label class="col-10 form-label">{{ model.app.branch }}</label>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Docs</label>
			<div class="col-10">
				<div v-for="item in model.app.docs"><span v-if="model.app.docs.length > 1">&bull;</span><a :href="item" target="_blank">{{ truncate(item) }}</a></div>
			</div>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Project Management</label>
  			<div class="col-10">
				<div v-for="item in model.app.pm">{{ item }}</div>
			</div>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Website</label>
			<span class="col-10"><a :href="model.app.webSite" target="_blank">{{ model.app.webSite }}</a></span>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Host</label>
  			<label class="col-10 form-label" v-html="formatP(model.app.host)"></label>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">App Type</label>
  			<label class="col-10 form-label" v-html="formatP(model.app.appType)"></label>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Stack</label>
  			<label class="col-10 form-label" v-html="formatP(model.app.stack)"></label>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Pipeline Environments</label>
  			<label class="col-10 form-label" v-html="formatP(model.app.env)"></label>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">IDM</label>
  			<label class="col-10 form-label" v-html="model.app.idm"></label>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Integrations</label>
  			<label class="col-10 form-label" v-html="model.app.integrations"></label>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Scale</label>
  			<label class=" col-10 form-label" v-html="formatP(model.app.scale)"></label>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Growth</label>
  			<label class="col-10 form-label" v-html="formatP(model.app.growth)"></label>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Availability</label>
  			<label class="col-10 form-label">{{ model.app.availability }}</label>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Comments/Notes</label>
  			<label class="col-10 form-label" v-html="formatP(model.app.comments)"></label>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Review</label>
			<span class="col-10"><a :href="model.app.review">{{ truncate(model.app.review) }}</a></span>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Last Update</label>
  			<label class="col-10 form-label" v-html="dateAsLocal(model.app.modified)"></label>
		</div>
	</div>

</div>

</template>

<style scoped>
div.row label:first-child{
	background-color: lightgray;

}
</style>

