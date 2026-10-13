<script setup>

import { useRoute, useRouter } from 'vue-router';
import { isEmpty, log } from '/src/js/utils.js'
import { getToken } from '/src/js/storage.js';
import AuthToken from "/src/js/security.js";
import { appConfig as config } from '/src/js/config.js'

//http://localhost:5173/app/get/27a0422c-a25b-472b-8e96-2882c74664ad

const route = useRoute();
const router = useRouter();

const last = route.query && !isEmpty(route.query.last) ? route.query.last : '';
//const last = !isEmpty(route.fullPath) ? base64Encode(route.fullPath, true) : '';
//const token =  new AuthToken( getToken() );

// if ( token.isValid )
//     router.push( { name: last } );
// else
//     window.location.replace(`${config.baseURL}api/login?id=${config.environment}&env=${config.environment}&last=${last}&code=${config.apiKey}`);

</script>

<template>
<div class="container">

    <div style="" class="w-25 mx-auto mt-5">
        <img src="/images/jacobs.svg" class="card-img-top mb-3" alt="Jacobs Logo">
        <div class="card text-center">
            <div class="card-header">Login to {{config.baseTitle}}</div>
            <div class="card-body">
                <div>
                    <a :href="`${config.baseURL}api/login?id=${config.environment}&env=${config.environment}&last=${last}&code=${config.apiKey}`" title="Login">Authenticate using Ping Identity</a>
                </div>
            </div>
            <div class="card-footer"><RouterLink class="card-link" :to="{name:'help'}">Help</RouterLink></div>
        </div>
    </div>

</div>
</template>
