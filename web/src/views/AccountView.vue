<script setup>

import { ref } from 'vue'
import { appConfig as config } from '/src/js/config.js'
import DataManager from '/src/js/datamanager.js';

let props = defineProps( { auth:Object } );

let ip = ref('fetching...');
const repo = new DataManager(config.baseURL, config.apiKey);

// ping with the users token
repo.ping('hello').then( data => {
    if (!data.isSuccess) return;
    ip.value = data.result.ip;
});

</script>

<template>
<div class="container">

    <p class="h3">User Account Info</p>

    <form>
        <div class="mb-2 row">
            <label class="col-1 col-form-label fw-bolder">IP Address</label>
            <label id="ipField" class="col-3 col-form-label">{{ ip }}</label>
        </div>
        <div class="alert alert-primary" role="alert">
            <h5 class="alert-heading">Token</h5>
            <p id="tokenField" class="text-break">{{ props.auth.encoded }}</p>
        </div>
        <textarea id="jwtField" cols="60" rows="20">{{ props.auth.jwt }}</textarea>
    </form>

</div>
</template>

