import { appConfig as config } from './config.js'
import DataManager from "./datamanager.js";
import * as uuid from 'uuid';

/* app model */
export class AppsModel {

	constructor() {
        this._lookups = null;
        this._repo = new DataManager(config.baseURL, config.apiKey);
    }

    /* data we need in the whole app */
    get lookups() {
        // no need to refetch
        if ( this._lookups )
            return this._lookups;
        
        return (async () => {
            try {
                const results = await this._repo.getLookups();
                if (results.isSuccess)
                {
                    this._lookups = results.result;
                    return this._lookups;
                }
            } catch(exp) {
                this._lookups = null;
                return {};
            }
        })();
    }

    /* get a list of all the apps */
    async listApps() {
        const results = await this._repo.listApps();
        if (results.isSuccess) return results.result;
        else if (results.login )
            window.location.replace(`/login`);
        else
            return [];
    }

    /* get a list of all the PoCs */
    async listPoCs() {
        const results = await this._repo.listPoCs();
        if (results.isSuccess) return results.result;
        else if (results.login )
            window.location.replace(`/login`);
        else
            return [];
    }

    /* get a list of all the Repos */
    async listRepos() {
        const results = await this._repo.listRepos();
        if (results.isSuccess) return results.result;
        else if (results.login )
            window.location.replace(`/login`);
        else
            return [];
    }

    /* get a list of all the Readmes */
    async listReadmes() {
        const results = await this._repo.listReadmes();
        if (results.isSuccess) return results.result;
        else if (results.login )
            window.location.replace(`/login`);
        else
            return [];
    }

    /* get a Readme */
    async getReadme(id) {
        const results = await this._repo.getReadme(id);
        if (results.isSuccess) return results.result;
        else if (results.login )
            window.location.replace(`/login`);
        else
            return [];
    }

    /* get a list of all the PoCs */
    async listUsers() {
        const results = await this._repo.listUsers();
        if (results.isSuccess) return results.result;
        else if (results.login )
            window.location.replace(`/login`);
        else
            return [];
    }

	/* get one app */
    async getApp(uuidStr) {
        if (!uuid.validate(uuidStr))
            return { id: uuid.NIL, name: 'App', isProd:false };

        const results = await this._repo.getApp(uuidStr);
        return results.isSuccess ? results.result : { id: uuid.NIL, name: 'App', isProd:false };
    }

    /* get next apps */
    async getPage(uuidStr) {
        const results = await this._repo.getPage(uuidStr);
        return results.result;
    }

	/* insert a new app 
    Returns the app ID
    */
    async insertApp(item) {
        const results = await this._repo.insertApp(item);
        return results.result;
    }

	/* update an app 
    Returns the new Updated date if success otherwise false
    */
    async updateApp(item) {
        const results = await this._repo.updateApp(item);
        return results.isSuccess ? results.result : false;
    }

	/* delete an app 
    Returns true if delete otherwise false
    */
    async deleteApp(uuidStr) {
        if (!uuid.validate(uuidStr))
            return false;

        const results = await this._repo.deleteApp(uuidStr);
        if (results.isSuccess) return true;
        if (results.login ) window.location.replace(`/401`);
    }
}