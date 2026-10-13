<script setup>

</script>

<template>

<div class="container">
    <p class="h3">Unauthorized</p>

	<p>You are NOT authorized to access this feature. You may:</p>

	<ul>
		<li><RouterLink :to="{name:'home'}">Just Go Home</RouterLink></li>
		<li><RouterLink :to="{name:'login'}">Login Again</RouterLink></li>
	</ul>

</div>

</template>