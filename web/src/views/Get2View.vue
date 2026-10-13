<script setup>

import { onMounted, ref } from 'vue'
import { appConfig as config } from '/src/js/config.js'
import { isEmpty, isURL, truncate, dateAsLocal } from '/src/js/utils.js'
import { useRoute, useRouter, onBeforeRouteUpdate } from 'vue-router';
import { AppsModel } from '/src/js/model.js'

const router = useRouter();
const route = useRoute();

let model = ref( { app:{ name:'App' }, waiting: false, page: {} } );
model.value.app.id = route.params.id;

const repo = new AppsModel();

/* when the view is mounted */
onMounted( async () => {
	getReadme();
});

/* watches the route for params change */
onBeforeRouteUpdate( async (to, from) => {
	if (to.params.id == from.params.id) return;
	model.value.app.id = to.params.id;
	getReadme();
});

/* get or reload an app */
function getReadme() {
	model.value.waiting = true;
	repo.getReadme(model.value.app.id)
	.then( json => {
		model.value.waiting = false;
		// reformat fields here instead of on each bind
		model.value.app = json;
		document.title = `${config.baseTitle}-${model.value.app.name}`;
	});

	// get the next page info
	//repo.getPage(model.value.app.id).then( json => { model.value.page = json; } );
}

/* replaces all newlines with a br tag */
const formatP = (str) => isEmpty(str) ? '' : str.replaceAll('\n', '<br/>');

</script>

<template>

<div class="container">
	<p class="h3"><span>{{ model.app.name }}</span><span class="ms-2">[{{ model.app.isProd ? 'Production' : 'Non-Production' }}]</span></p>

	<div class="btn-group mb-3" role="group">
		<!-- <button type="button" class="btn btn-secondary" @click.stop="router.push( { name: 'get', params:{id:model.page.prev} } )" title="Previous">
			<i class="bi bi-arrow-left-circle-fill"></i>
		</button> -->
		<button type="button" class="btn btn-primary" @click.stop="router.push( { name: 'home' } )" title="Back to List">
			<span class="spinner-border spinner-border-sm" :class="{ 'visually-hidden': !model.waiting }" role="status" aria-hidden="true"></span>
			<span :class="{ 'visually-hidden': model.waiting }"><i class="bi bi-arrow-up-circle-fill"></i></span>
		</button>
		
		<!-- <button type="button" class="btn btn-secondary" @click.stop="router.push( { name: 'get', params:{id:model.page.next} } )" title="Next">
			<i class="bi bi-arrow-right-circle-fill"></i>
		</button>
		<button type="button" class="btn btn-primary" @click.stop="router.push( { name: 'update', params:{id:model.app.id} } )">
			<span class="spinner-border spinner-border-sm" :class="{ 'visually-hidden': !model.waiting }" role="status" aria-hidden="true"></span>
			<span :class="{ 'visually-hidden': model.waiting }">Edit</span>
		</button> -->
	</div>

	<div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Aliases</label>
  			<label class=" col-10 form-label" v-html="model.app.alias?.join(', ')"></label>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Description</label>
  			<label class="col-10 form-label" v-html="formatP(model.app.desc)"></label>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Repo</label>
			<span class="col-10" v-if="isURL(model.app.url)"><a :href="model.app.url" target="_blank">{{ model.app.url }}</a></span>
			<span class="col-10" v-else>missing repo</span>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Team</label>
			<div class="col-10">
				<div v-for="item in model.app.team"><span v-if="model.app.team?.length > 1">&bull;</span>{{ `[${item.role}] ${item.name}` }}</div>
			</div>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Other Repos</label>
  			<div class="col-10">
				<div v-for="item in model.app.children"><span v-if="model.app.children?.length > 1">&bull;</span><a :href="item.url" target="_blank">{{ item.url }}</a></div>
			</div>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Prod Branch</label>
  			<label class="col-10 form-label">{{ model.app.prodBranch }}</label>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Docs</label>
			<div class="col-10">
				<div v-for="item in model.app.docs">
                    <span v-if="model.app.docs?.length > 1">&bull;</span><a :href="item.url" target="_blank">{{ truncate(item.label) }}</a>
                </div>
			</div>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Project Management</label>
  			<span class="col-10"><a :href="model.app.proj?.url" target="_blank">{{ model.app.proj?.label }}</a></span>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Website</label>
			<span class="col-10"><a :href="model.app.website?.url" target="_blank">{{ model.app.website?.label }}</a></span>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Availability</label>
			<span class="col-10">{{ `${model.app.uptime}%` }}</span>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Stack</label>
			<div class="col-10">
				<ul>
					<li v-for="item in model.app.stack"><span>{{ item }}</span></li>
				</ul>
			</div>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Branch Commits</label>
			<div class="col-10">
				<ul>
					<li v-for="item in model.app.branches"><span>{{ `${item.name} [${dateAsLocal(item.ts)}] ` }}</span></li>
				</ul>
			</div>
		</div>
		<div class="row mb-1">
			<label class="col-2 form-label fw-bolder">Last Update</label>
  			<label class="col-10 form-label" v-html="dateAsLocal(model.app.ts)"></label>
		</div>
	</div>

</div>

</template>

<style scoped>
div.row label:first-child{
	background-color: lightgray;

}
</style>

