<script setup>

import { onMounted, ref, watch } from 'vue'
import { appConfig as config } from '/src/js/config.js'
import { isEmpty, isAnyEmpty, clamp } from '/src/js/utils.js'
import { useRoute, useRouter } from 'vue-router';
import { AppsModel } from '/src/js/model.js'
import * as uuid from 'uuid';
import HelpModal from '/src/components/HelpModal.vue';
import { helpMessages } from '/src/js/messages.js'
import { Modal as bsModal } from 'bootstrap'

const router = useRouter();
const route = useRoute();

let props = defineProps( { auth:Object } );

let model = ref( { 
	app:{ id:uuid.NIL, name:'My App Name', isProd:false },
	lookups: { availability:[] },
	state:route.name,
	waiting: false,
	helpMsg:''
});

const isAdmin = props.auth.isAuthorized('admin');

const repo = new AppsModel();

/* when the view is mounted */
onMounted( async () => {

	model.value.lookups = await repo.lookups;

	if ( model.value.state === 'update' )
	{
		model.value.app.id = route.params.id;
		getApp();
	}
});

/* the route action changed 
switch to using onBeforeRouteUpdate
*/
watch( () => route.name, (value) => {
    model.value.state = value;
	if ( model.value.state === 'update' )
	{
		// refetch the app data
		getApp();
	}
	else if ( model.value.state === 'insert' )
		model.value.app = { name:'New', isProd:false };
});

/* get or reload an app */
function getApp() {
	model.value.waiting = true;
	repo.getApp(model.value.app.id)
	.then( json => {
		model.value.waiting = false;
		model.value.app = json;
		document.title = `${config.baseTitle}-${model.value.app.name}`;
	});
}

/* upper case the first letter in the string */
const capitalized = (s) => `${s.charAt(0).toUpperCase()}${s.slice(1)}`;

/* insert or update an app based on current state */
function doEdit(event)
{
	if ( isAnyEmpty([model.value.app.name]))
		return;

	model.value.waiting = true;

	if ( model.value.state === 'insert')
	{
		repo.insertApp(model.value.app).then( json => {
			model.value.waiting = false;
			if ( json == uuid.NIL) return;
			model.value.app.id = json;
			router.push( { name: 'update', params:{id:json} } );
		});
	}
	else // do update
	{
		repo.updateApp(model.value.app).then( json => {
			model.value.waiting = false;
			if (!json)
				alert('App was NOT updated!');
			else
			{
				model.value.app.modified = json;
				alert('The app was updated');
			}
		});
	}
}

/* splits a text box into an array of lines */
function text2Array(event, field, max) {

	if ( isEmpty(field) ) return;

	//const max = clamp(parseInt(event.target.rows), 1, 10);
	max = clamp(max, 1, 10);

	let lines = event.target?.value.split('\n', max);

	model.value.app[field] = [];

	lines.forEach(l => {
		l = l.trim();
		if ( !isEmpty(l) )
			model.value.app[field].push(l);
	});
}

/* concats an array of strings back to a single string */
function array2text(field) {
	if ( isEmpty(field) ) return;

	if ( !model.value.app[field] )
	{
		model.value.app[field] = [];
		return '';
	}

	return model.value.app[field].join('\n');
}

/* display the help modal */
const doShowHelp = (msgID) => {
	model.value.helpMsg = helpMessages[msgID];
    new bsModal('#helpModal').show();
}

</script>

<template>

<div class="container">
    <p class="h3">{{ `${capitalized(model.state)} ${model.app.name}` }}</p>

	<div class="btn-group mb-3" role="group">
		<button type="button" class="btn btn-primary" @click.stop="doEdit">
			<span class="spinner-border spinner-border-sm" :class="{ 'visually-hidden': !model.waiting }" role="status" aria-hidden="true"></span>
			<span :class="{ 'visually-hidden': model.waiting }">Save</span>
		</button>
		<button v-if="model.state === 'update'" type="button" class="btn btn-secondary" @click.stop="router.push( { name: 'get', params:{id:model.app.id} } )">View</button>
	</div>

	<form>
		<div class="mb-3 form-check">
			<label class="form-check-label fw-bold" for="prodField">In Production</label><i class="bi bi-question-circle ms-1" @click.stop="doShowHelp('prodHelp')"></i>
			<input type="checkbox" class="form-check-input" id="prodField" :checked="model.app.isProd" @input.stop="evt => model.app.isProd = evt.target.checked">
		</div>
		<div class="mb-3">
			<span><label for="nameField" class="form-label fw-bold">What is the final/official name of this app/project?</label><i class="bi bi-question-circle ms-1" @click.stop="doShowHelp('nameHelp')"></i></span>
  			<input type="text" class="form-control" id="nameField" v-model="model.app.name">
		</div>
		<div class="mb-3">
			<span><label for="aliasField" class="form-label fw-bold">List any aliases this app may go by:</label><i class="bi bi-question-circle ms-1" @click.stop="doShowHelp('aliasHelp')"></i></span>
			<textarea class="form-control" id="aliasField" maxlength="2048" rows="2" @change="evt => text2Array(evt,'aliases',3)" :value="array2text('aliases')"></textarea>
		</div>
		<div class="mb-3">
			<span><label for="teamField" class="form-label fw-bold">List the people on the team:</label><i class="bi bi-question-circle ms-1" @click.stop="doShowHelp('teamHelp')"></i></span>
			<textarea class="form-control" id="teamField" maxlength="2048" rows="6" @change="evt => text2Array(evt,'team',20)" :value="array2text('team')"></textarea>
		</div>
		<div class="mb-3">
			<span><label for="descField" class="form-label fw-bold">Describe the app:</label><i class="bi bi-question-circle ms-1" @click.stop="doShowHelp('descHelp')"></i></span>
			<textarea class="form-control" id="descField" maxlength="8192" rows="4" v-model="model.app.description" ></textarea>
		</div>
		<div class="mb-3">
			<span><label for="repoField" class="form-label fw-bold">List the app's repositories:</label><i class="bi bi-question-circle ms-1" @click.stop="doShowHelp('reposHelp')"></i></span>
			<textarea class="form-control" id="repoField" maxlength="2048" rows="3" @change="evt => text2Array(evt,'repos',5)" :value="array2text('repos')"></textarea>
		</div>
		<div class="mb-3">
			<span><label for="branchField" class="form-label fw-bold">What is the Production branch?</label><i class="bi bi-question-circle ms-1" @click.stop="doShowHelp('branchHelp')"></i></span>
  			<input type="text" class="form-control" id="branchField" v-model="model.app.branch">
		</div>
		<div class="mb-3">
			<span><label for="docsField" class="form-label fw-bold">List links to all documents:</label><i class="bi bi-question-circle ms-1" @click.stop="doShowHelp('docsHelp')"></i></span>
			<textarea class="form-control" id="docsField" maxlength="2048" rows="5" @change="evt => text2Array(evt,'docs',10)" :value="array2text('docs')"></textarea>
		</div>
		<div class="mb-3">
			<span><label for="pmField" class="form-label fw-bold">Link(s) to the Project Management Dashboard:</label><i class="bi bi-question-circle ms-1" @click.stop="doShowHelp('pmHelp')"></i></span>
			<textarea class="form-control" id="pmField" maxlength="2048" rows="3" @change="evt => text2Array(evt,'pm',5)" :value="array2text('pm')"></textarea>
		</div>
		<div class="mb-3">
			<span><label for="websiteField" class="form-label fw-bold">App Website (if any)</label><i class="bi bi-question-circle ms-1" @click.stop="doShowHelp('websiteHelp')"></i></span>
  			<input type="text" class="form-control" id="websiteField" v-model="model.app.webSite">
		</div>
		<div class="mb-3">
			<span><label for="hostField" class="form-label fw-bold">How is this app hosted?</label><i class="bi bi-question-circle ms-1" @click.stop="doShowHelp('hostHelp')"></i></span>
			<textarea class="form-control" id="hostField" maxlength="2048" rows="3" v-model="model.app.host"></textarea>
		</div>
		<div class="mb-3">
			<span><label for="typeField" class="form-label fw-bold">What type of app is this (see help)?</label><i class="bi bi-question-circle ms-1" @click.stop="doShowHelp('typeHelp')"></i></span>
  			<input type="text" class="form-control" id="typeField" v-model="model.app.appType">
		</div>
		<div class="mb-3">
			<span><label for="stackField" class="form-label fw-bold">Itemize the tech stack:</label><i class="bi bi-question-circle ms-1" @click.stop="doShowHelp('stackHelp')"></i></span>
			<textarea class="form-control" id="stackField" maxlength="2048" rows="7" v-model="model.app.stack"></textarea>
		</div>
		<div class="mb-3">
			<span><label for="envField" class="form-label fw-bold">Pipeline Environments?</label><i class="bi bi-question-circle ms-1" @click.stop="doShowHelp('envHelp')"></i></span>
  			<input type="text" class="form-control" id="envField" v-model="model.app.env">
		</div>
		<div class="mb-3">
			<span><label for="idmField" class="form-label fw-bold">What Identity Management Provider does the app use (see help)?</label><i class="bi bi-question-circle ms-1" @click.stop="doShowHelp('idmHelp')"></i></span>
  			<input type="text" class="form-control" id="idmField" v-model="model.app.idm">
		</div>
		<div class="mb-3">
			<span><label for="idmField" class="form-label fw-bold">Does the app have any data integrations (see help)?</label><i class="bi bi-question-circle ms-1" @click.stop="doShowHelp('integrationsHelp')"></i></span>
			<textarea class="form-control" id="integrationsField" maxlength="4096" rows="3" v-model="model.app.integrations"></textarea>
		</div>
		<div class="mb-3">
			<span><label for="scaleField" class="form-label fw-bold">What is the current “scale” of the app?</label><i class="bi bi-question-circle ms-1" @click.stop="doShowHelp('scaleHelp')"></i></span>
			<textarea class="form-control" id="scaleField" maxlength="2048" rows="3" v-model="model.app.scale"></textarea>
		</div>
		<div class="mb-3">
			<span><label for="growthField" class="form-label fw-bold">What is the expected growth of the app?</label><i class="bi bi-question-circle ms-1" @click.stop="doShowHelp('growthHelp')"></i></span>
			<label for="growthField" class="form-label fw-bold"></label>
			<textarea class="form-control" id="growthField" maxlength="2048" rows="3" v-model="model.app.growth"></textarea>
		</div>
		<div class="mb-3">
			<span><label for="availabilityField" class="form-label fw-bold">How long can the app be down per incident?</label><i class="bi bi-question-circle ms-1" @click.stop="doShowHelp('availabilityHelp')"></i></span>
			<select class="form-select" id="availabilityField" v-model="model.app.availability">
                <option v-for="item in model.lookups.availability" :value="item.key">{{ item.desc }}</option>
            </select>
		</div>
		<div class="mb-3">
			<span><label for="commentsField" class="form-label fw-bold">Notes and Comments</label><i class="bi bi-question-circle ms-1" @click.stop="doShowHelp('commentsHelp')"></i></span>
			<textarea class="form-control" id="commentsField" maxlength="8192" rows="5" v-model="model.app.comments"></textarea>
		</div>
		<div v-if="isAdmin" class="mb-3">
			<span><label for="reviewField" class="form-label fw-bold">Loop Workspace URL</label></span>
  			<input type="text" class="form-control" id="reviewField" v-model="model.app.review">
		</div>
	</form>

	<div class="btn-group mb-5" role="group">
		<button type="button" class="btn btn-primary" @click.stop="doEdit">
			<span class="spinner-border spinner-border-sm" :class="{ 'visually-hidden': !model.waiting }" role="status" aria-hidden="true"></span>
			<span :class="{ 'visually-hidden': model.waiting }">Save</span>
		</button>
		<button v-if="model.state === 'update'" type="button" class="btn btn-secondary" @click.stop="router.push( { name: 'get', params:{id:model.app.id} } )">View</button>
	</div>

	<HelpModal :title="'Help'" :msg="model.helpMsg" />

</div>

</template>
