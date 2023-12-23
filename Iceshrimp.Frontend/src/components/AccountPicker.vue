<script setup lang="ts">
import { onMounted, ref } from "vue";
import { KvAccount } from "../entities/keyval.ts";
import { get as kvGet } from "idb-keyval";

const test = ref<string[]>([]);
const aref = ref<HTMLSelectElement>();

onMounted(async () => {
	const accounts = await kvGet<KvAccount[] | null>("accounts");
	console.log(accounts);
	if (!accounts) return;
	test.value.push(...accounts.map(p => p.id));
});

async function submit() {
	localStorage.setItem('accountId', aref.value!.value);
}
</script>

<template>
	<select ref="aref">
		<option v-for="item in test" :key="item">
			{{ item }}
		</option>
	</select>
	<button @click="submit">
		Submit
	</button>
</template>

<style scoped lang="scss">

</style>
