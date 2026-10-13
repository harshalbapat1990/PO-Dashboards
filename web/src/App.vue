<script setup>

import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { appConfig as config } from '/src/js/config.js'
import { isEmpty } from '/src/js/utils.js'
//import base64Encode from '/src/js/base64.js'
import { encode } from 'js-base64';
import { getToken, deleteToken, eraseCookie } from '/src/js/storage.js';
import AuthToken from "/src/js/security.js";
import Header from './components/Header.vue';

let route = useRoute();
let router = useRouter();

let token = ref( new AuthToken('') );

// check for token on every route change
router.beforeEach( async (to) => {

	document.title = config.baseTitle;

	// clear all cookies and stored data and go home
	if ( to.name === 'erase' )
	{
		deleteToken();
		router.push( { name: 'home' } );
	}

	// fetch the local token
	token.value = new AuthToken( getToken() );

	// we want to intercept logout to make sure OUR token is removed no matter what happens
	if ( to.name === 'logout' )
	{
		// if not logged in just go to home
		if ( !token.value.loggedIn )
      		return { name: 'home' };
		else
		{
			deleteToken();
			token.value = new AuthToken('');
		
			// redirect to the OAuth logout URL from our API
			window.location.replace(`${config.baseURL}api/logout?id=${config.environment}`);
		}
	}

	// if it requirea auth and above Dev
	if ( to.meta.requiresAuth )
  	{
		// test the token is legit against the backend key
    	//const repo = new DataManager(config.baseURL, config.apiKey);
    	//token.value = await repo.authenticate();

		// if true if A&A passes otherwise send to the login page
		//const last = !isEmpty(to.fullPath) ? encode(to.fullPath, true) : '';
		const last = !isEmpty(to.fullPath) ? to.fullPath : '';
		return token.value.isValid ? true : { name: 'login', query: { last: last } };
	}

	return true;

})

</script>

<template>

	<Header v-if="route.name != 'login'" :auth="token"></Header>

	<!-- body container -->
	<RouterView :auth="token" />

</template>