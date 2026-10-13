# App Catalog

## Description

App Catalog collects metadata on DA Apps from GitHub using the GitHub API.

### Metadata
Required fields. Bolding the key is optional but only use a colon `:` as the delimiter for now. Links can be the simple or Markdown format.

- **Is Production** : true
- **Production Branch** : main
- **Project Management** : https://github.com/People-Places-Solutions/DesignAutomation.AppCatelog/issues
- **Website** : https://appcatalog.jacobs.com
- **Availability** : 95.0%

### Other Repos

* https://github.com/People-Places-Solutions/DesignAutomation.README-Template

### Use Case

We need to filter out all the GTE repos including DA, AoD, and TS from the 2000 GitHub repos and dashboard them in one easy to see place.
This dashboard just pulls GTE repos and parses some info from the README to surface here. It will also show some basic metrics in one place.

### Size

Whatever the headcount of GTE is.

### Availability

Downtime should not exceed 2 days.

## Team

| Member                       | Role               |
| ---------------------------- | ------------------ |
| tag.guillory@jacobs.com      | Lead Developer     |

## Documents

* [Software Architecture Documents](https://jacobsengineering.sharepoint.com/:fl:/r/contentstorage/x8FNO-xtskuCRX2_fMTHLaA_XwPbq5JAlMfaM3OGxMA/Document%20Library/Copilot/Software%20Design%20Process.loop?d=w0adfb6ebe5f245fbabb6843a59ac0653&csf=1&web=1&e=wV2f92&nav=cz0lMkZjb250ZW50c3RvcmFnZSUyRng4Rk5PLXh0c2t1Q1JYMl9mTVRITGFBX1h3UGJxNUpBbE1mYU0zT0d4TUEmZD1iJTIxMnFrSmVPS2NvRVMwZmNQQmZWMkloVW9WelB1N2Z1NUR1VmRLZTUyczQxNE5Na01VVEg4SFI0ckdNaTNZWE1LLSZmPTAxWU80QldDSExXM1BRVjRYRjdOQzJYTlVFSEpNMllCU1QmYz0lMkYmYT1Mb29wQXBwJnA9JTQwZmx1aWR4JTJGbG9vcC1wYWdlLWNvbnRhaW5lciZ4PSU3QiUyMnclMjIlM0ElMjJUMFJUVUh4cVlXTnZZbk5sYm1kcGJtVmxjbWx1Wnk1emFHRnlaWEJ2YVc1MExtTnZiWHhpSVRKeGEwcGxUMHRqYjBWVE1HWmpVRUptVmpKSmFGVnZWbnBRZFRkbWRUVkVkVlprUzJVMU1uTTBNVFJPVFd0TlZWUklPRWhTTkhKSFRXa3pXVmhOU3kxOE1ERlpUelJDVjBORVdVeFVUMVpPVkVWT1FWWkNXVVpCV0RKTFNGazBSVlJETnclM0QlM0QlMjIlMkMlMjJpJTIyJTNBJTIyNmMyOTY0NTUtNzZlZi00NTk1LWJhOTctYTcyNTRlMWFkYmRjJTIyJTdE)

# Tech Stack

<!-- Descibe the primary tech stack being used -->

* C#/ASP.NET 10
* vue.js 3
* Mongo
* Ping
* Octokit/GitHub API

# Project Setup

## Making Changes

- Make a new branch for your changes
- Commit your changes and Push to the new Branch
  - For now - all web app changes are automatically deployed to the GUI
- Run the Dev API Action to publish to Development
- Test using a local Web App against the Dev APIs
- If Testing is accepted, then run the Production API Action

## Setting up a New App

[Vite Setup](https://vitejs.dev/guide/)

Create a new app folder and drop that folder onto VS Code.

Make sure you have the official vue extension installed.

Create two folders at the top level of the project:
+ api
+ web

## Installing vue.js & vite

change to the web folder and install vite

```sh
npm install -D vite
```

Now template your first vue web app using

```sh
npm create vite@latest . -- --template vue
```
Say yes to installing `create-vite`.
Ignore any existing files in the folder as they are probably just hidden OS files.

Move the **README.md** and **.gitignore** files to the top level of the project.

Open **package.json** and edit the app name and version.

## Add in vue-router, Bootstrap and sass

```sh
npm install vue-router@latest
npm install bootstrap@latest
```

You may get an error trying to update or install npm packages in the global cache. Delete the cache using
```sh
cd /Users/<my_id>/.npm
sudo rm -rf _cacache
npm update
```

### Installing other Packages

to install and uninstall a package
```sh
npm install <package_name>
npm uninstall <package_name>
```

## Run the App

A basic app should work now. Then update all packages and run locally using:

```sh
npm update
npm run dev
```

If you want to build the web site locally you can use
```sh
npm run build
```

## Add a Settings File

You will need to manually add a `.env.local` settings file to the root of the web app similar to the APIs. You can get the values from another developer or copy the YML `env:` section **only**. DO NOT commit this file to the repo. The YML script will create a new one on the service each time.

```
VITE_baseURL: ''
VITE_apiKey: ''
```

When running locally, you must choose an API to point to in the baseURL setting. If you use Localhost APIs then an API Key and Token are not required.

## Restting Packages

This will remove your local node packages, clean the caches and re-install them all

```sh
rm -rf node_modules
npm cache clean --force
npm install
```

## Running the APIs

First you must create a new .NET isolated Azure Function in the api folder using whatever method you like. It's easiest to install the Azure Function extension in Visual Studio Code.

[Azure Functions Documentation](https://learn.microsoft.com/en-us/azure/azure-functions/how-to-create-function-azure-cli?pivots=programming-language-csharp&tabs=windows%2Cbash%2Cazure-cli)

```sh
func init --worker-runtime dotnet-isolated
```

Run the functions using:

```sh
cd api/<api_proj_dir>
func start
```

to debug

```sh
cd api/<api_proj_dir>
func host start --dotnet-isolated-debug
```

After build the terminal will block waiting for the debugger - use Command-Shift-P and choose debugger and attach to the running process (usually the first one listed).

## CORS

If you don't bind the API to the Static Web App you will need to modify CORS in the Azure Function to include all possible client origination domains.

## ID Management

Use Ping to authenticate a user

[Ping Docs](https://apidocs.pingidentity.com/pingone/auth/v1/api/#get-authorize-authorization_code-get)

### Getting a Test Token

Use Postman to login using the OAuthLogin method with query parameters set to
```
?echo=true&method=scrape
```
This will give you a complete Login URL which you can cut and paste to the browser and login and ultimately get the token back in the same browser window.

## Swagger Documentation

Add `Microsoft.Azure.Functions.Worker.Extensions.OpenApi` to automatically create Swagger API Doc endpoints.

## Cleanup

You can remove any **.vscode** folders that are NOT at the top level of the project.