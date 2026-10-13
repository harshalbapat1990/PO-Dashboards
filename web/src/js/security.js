/* test a string is null or whitespace only */
const isEmpty = (s) => s === null || s === undefined ? true : /^[\s\xa0]*$/.test(s);

/* Holds the JWT and converts as necessary */
export default class AuthToken {

    constructor(tokenStr) {
        this.encoded = tokenStr;
        this.jwt = this.decoded;
    }

    /* the JWT exp field is an Epoch time Unix timestamp which is expressed in seconds that have elapsed since 1970-01-01 00:00:00 GMT. */
    get expires() { return this.jwt.exp }

    /* returns the user's internal DB ID usually a UUID */
    get userID() { return this.jwt.id }

    /* returns the user's alias ID 
    DEPRECATED - removed from the token
    user getUserInfo
    */
    get alias() { return this.jwt.userid }

    /* returns the path to the users profile image */
    get picture() { return this.jwt.picture }

    /* tests for a valid token ONLY does not do an auth check */
    get isValid() {
        if (typeof this.encoded !== 'string' || isEmpty(this.encoded) )
            return false;

        if (this.jwt == null || typeof this.jwt !== 'object' || !this.jwt.exp )
            return false;

        // check expiry data - the jwt exp seconds were generated from UTC time - is date now in UTC?
        return Date.now() < (this.jwt.exp * 1000);
    }

    /* legacy for isValid which does the exact same thing */
    get loggedIn() { return this.isValid; }

    /* the encoded token string */
    get token() { return this.encoded; }

    /* Decodes a JWT string token back into a JSON object */
    get decoded() {

        if ( isEmpty(this.encoded) ) return null;

        let base64Url = this.encoded.split('.')[1];
        let base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');

        let jsonPayload = decodeURIComponent(window.atob(base64).split('').map( function (c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));

        return JSON.parse(jsonPayload);
    }

    /* does this token have this role 
    */
    isAuthorized( roleName )
    {
        if (!this.isValid || isEmpty(roleName) || !this.jwt.role )
            return false;

        roleName = roleName.trim().toLowerCase();

        const role = AuthToken.roles.find(r => r.value.toLowerCase() === roleName );
        const user = AuthToken.roles.find(r => r.value.toLowerCase() === this.jwt.role.toLowerCase());

        if ( !role || !user )
            return false;

        return user.key >= role.key;
    }

    // role enums
    static Guest = { key: 0, value: "guest" };
    static User = { key: 1, value: "user" };
    static Editor = { key: 10, value: "editor" };
    static Admin = { key: 90, value: "admin" };

    static roles = [AuthToken.Guest, AuthToken.User, AuthToken.Editor, AuthToken.Admin];
}
