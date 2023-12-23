<script setup lang="ts">
import { api } from "../helpers/api.ts";
import { onMounted, ref } from "vue";

const field = ref<HTMLElement>();

onMounted(() => {
	api('/v1/auth').then(res => {
		field.value!.textContent = JSON.stringify(res, null, 2);
	});
});
</script>

<template>
	<pre ref="field" class="auth" />
</template>

<style scoped lang="scss">
.auth {
	text-align: left;
}
</style>
