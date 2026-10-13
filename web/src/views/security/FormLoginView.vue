<script setup>

import { ref } from 'vue'
import { setToken, deleteToken } from '/src/js/storage.js'
import { appConfig as config } from '/src/js/config.js'
import DataManager from '/src/js/datamanager.js';
import { useRouter, useRoute } from 'vue-router'
import { isEmpty, isAnyEmpty } from "/src/js/utils.js";
//import base64Encode from "/src/js/base64.js";
import { encode } from 'js-base64';

const router = useRouter();
const route = useRoute();

let model = ref({
    creds: { id:'', pw:'' },
    waiting: false,
    last: 'home' // the last secure page the user tried to go to
});

// get any attempted login page
if ( !isEmpty(route.query.last) )
    model.value.last = route.query.last;

// delete any existing token
deleteToken();

// create a repo and get the CSRF token
let repo = new DataManager(config.baseURL, config.apiKey);

/* test for a valid form */
const isValid = () => {

    if ( isAnyEmpty([model.value.creds.id, model.value.creds.pw]) )
        return false;

    return model.value.creds.id.length > 2 && model.value.creds.pw.length > 7;
};

/*  */
function getDate(days = 7) {
  const newDate = new Date(); 
  newDate.setDate(newDate.getDate() + days);
  return newDate.toISOString();
}

/* do the actual login */
function doLogin(event)
{
    if (!isValid()) return;

    //model.value.waiting = true;
    deleteToken();
    const token = encode(`${model.value.creds.id}:${model.value.creds.pw}`);
    setToken(token, getDate(14) );
    //repo.login(model.value.creds).then(loginCallback);
    router.push( { name:'home' } )
}

</script>

<template>

<div class="container">

    <p class="h3 mt-2">Login</p>

    <form class="mt-5">
        <div class="row mb-2">
            <div class="col-2">
                <label for="nameField" class="col-form-label">Jacobs eMail</label>
            </div>
            <div class="col-4">
                <input type="text" id="nameField" class="form-control" v-model.trim="model.creds.id" required @keydown.enter.prevent="doLogin" />
            </div>
        </div>
        <div class="row mb-4">
            <div class="col-2">
                <label for="pwField" class="col-form-label">Password</label>
            </div>
            <div class="col-4">
                <input type="password" id="pwField" class="form-control" v-model="model.creds.pw" required @keydown.enter.prevent="doLogin" />
            </div>
        </div>
        <div class="row">
            <button id="submitButton" type="button" class="btn btn-primary col-2" :disabled="!isValid()" @click.prevent="doLogin">
                <span id="waiter" class="spinner-border spinner-border-sm me-2" :class="{ 'visually-hidden': !model.waiting }" role="status" aria-hidden="true"></span>
                <span>Login</span>
            </button>
        </div>
    </form>

</div>

</template>
