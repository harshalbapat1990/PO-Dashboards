import { getToken } from "./storage.js";
import AuthToken from "./security.js";

/* test a string is null or whitespace only */
const isEmpty = (s) => s === null || s === undefined ? true : /^[\s\xa0]*$/.test(s);

/* Handles API calls with the backend */
export default class DataManager {
    constructor(url, key) {
        this.baseURL = url;
        this.apiKey = key;
    }

	/* ping the API */
    async ping(message) {
        return await this.getJSONAsync(`api/v1/ping?msg=${message}`, true);
    }

    /* get all the lookup menus */
    async getLookups() {
        return await this.getJSONAsync(`api/v1/menus`);
    }

    /* get a list of all the apps */
    async listApps() {
        return await this.getJSONAsync(`api/v1/apps`);
    }

    /* get a list of all the PoCs only */
    async listPoCs() {
        return await this.getJSONAsync(`api/v1/pocs`);
    }

    /* get a list of all the repos only */
    async listRepos() {
        return await this.getJSONAsync(`api/v1/repos`);
    }

    /* get a list of users */
    async listUsers() {
        return await this.getJSONAsync(`api/v1/users`);
    }

    /* get a list of Readmes */
    async listReadmes() {
        return await this.getJSONAsync(`api/v1/github`);
    }

    /* get Readme */
    async getReadme(id) {
        return await this.getJSONAsync(`api/v1/github/${id}`);
    }

    /* gets the next/previous app IDs  */
    async getPage(id) {
        return await this.getJSONAsync(`api/v1/apps/${id}`);
    }

    /* get a specific app */
    async getApp(id) {
        return await this.getJSONAsync(`api/v1/app/${id}`);
    }

    /* insert an app */
    async insertApp(data) {
        return await this.postJSONAsync(`api/v1/app`, data, false, 'Post');
    }

    /* update an app */
    async updateApp(data) {
        return await this.postJSONAsync(`api/v1/app`, data, false, 'Put');
    }

    /* delete an app */
    async deleteApp(id) {
        return await this.getJSONAsync(`api/v1/app/${id}`, false, 'Delete');
    }

	/* performs a get or delete request with a JSON or Text response */
    async getJSONAsync(url, anon = false, method = 'Get', format = 'json') {

        if (isEmpty(url)) return;

        const hedrs = new Headers();
        hedrs.append('x-timestamp', new Date().toISOString());
        hedrs.append('x-functions-key', this.apiKey);

        //hedrs.append('Authorization', `Basic ${getToken()}`);

        const auth = new AuthToken( getToken() );
        if ( !anon && auth.isValid ) {
            hedrs.append('Authorization', `Bearer ${auth.encoded}`);
        }

        try {
            const resp = await fetch( `${this.baseURL}${url}`, { method: method, headers: hedrs } );

            if (resp.ok)
                return (format !== 'json') ? await resp.text() : await resp.json();

            console.log(resp.status);

            // throw new Error(`Response status: ${response.status}`);
            return { isError: true, isSuccess: false, response: resp.status, login: resp.status == 401 };
            
        } catch (error) {
            console.error(error.message);
        }
    }

    /* performs a post, put or patch request with a JSON response */
    async postJSONAsync(url, data, anon = false, method = 'Post') {

        if (isEmpty(url)) return;

        const hedrs = new Headers();
        hedrs.append('x-timestamp', new Date().toISOString());
        hedrs.append('x-functions-key', this.apiKey);
        //hedrs.append('Authorization', `Basic ${getToken()}`);

        const auth = new AuthToken(getToken());
        if (!anon && auth.isValid) {
            hedrs.append('Authorization', `Bearer ${auth.encoded}`);
        }

        let options = {
            method: method,
            headers: hedrs,
            body: JSON.stringify(data)
        };

        try {
            const resp = await fetch(`${this.baseURL}${url}`, options );

            if (resp.ok)
                return await resp.json();

            console.log(resp.status);
            // throw new Error(`Response status: ${response.status}`);
            return { isError: true, isSuccess: false, response: resp.status, login: resp.status == 401 };

        } catch (error) {
            console.error(error.message);
        }
    }
}