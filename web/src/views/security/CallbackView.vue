<!-- the view sent to after OAuth login is complete to get the final token and store locally -->
<script setup>

import { useRoute, useRouter } from 'vue-router';
import { isEmpty, log } from '/src/js/utils.js'
import { setToken } from '/src/js/storage.js';
import { decode } from 'js-base64';
import AuthToken from "/src/js/security.js";

const route = useRoute();
const router = useRouter();

let loginMsg = 'Login Failed';

let last = '';
if ( route.query && !isEmpty(route.query.last) )
    //last = decode(route.query.last);
    last = route.query.last;

// the token is in the url as 'jwt' not a JSON object in the body
if ( route.query && !isEmpty(route.query.jwt) )
{
    const token = new AuthToken(route.query.jwt);

    if (token.isValid)
    {
        setToken(token.encoded, token.expires * 1000);
        // push to where they were going
        router.push( last );
    }
    else
    {
        loginMsg = 'Received an Invalid Token!';
        log('invalid token after login');
    }
    
}

</script>

<template>
<div class="container">

    <p>{{ loginMsg }}</p>

</div>
</template>