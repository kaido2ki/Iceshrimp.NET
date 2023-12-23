import { get as kvGet } from "idb-keyval";
import { KvAccount } from "../entities/keyval.ts";

export async function api(endpoint: string, body?: object, prefix: string = '/api/iceshrimp') {
    const token = (await getCurrentAccount())?.token ?? null;
	const headers: Record<string, string> = {};

	if (token != null) headers['Authorization'] = `Bearer ${token}`;
	if (body != null) headers['Content-Type'] = `application/json`;

    const request = {
        method: body ? 'POST' : 'GET',
        headers: headers,
        body: body ? JSON.stringify(body) : undefined
    };

    return fetch(prefix + endpoint, request).then(res => res.json());
}

//FIXME: cache this somewhere?
async function getCurrentAccount(): Promise<KvAccount | null> {
    const currentAccountId = localStorage.getItem('accountId');
    if (currentAccountId === null) return null;
    const accounts = await kvGet<KvAccount[] | null>("accounts");
    if (!accounts) return null;
    return accounts.find(p => p.id === currentAccountId) ?? null;
}
