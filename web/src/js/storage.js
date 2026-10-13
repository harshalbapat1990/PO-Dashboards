export const tokenKey = 'token';

/* 
cname - cookie name
cvalue - cookie value
exdays - days to expire in
*/
export function setCookie(cname, cvalue, exdays) {
    // calc milliseconds from days
    const d = new Date();
    d.setTime(d.getTime() + (exdays * 24 * 60 * 60 * 1000));

    let expires = "expires=" + d.toUTCString();
    document.cookie = cname + "=" + cvalue + ";" + expires + ";Same-Site=Strict;path=/";
}

/** */
export function getToken() { return getCookie(tokenKey); }

/** */
export function deleteToken() { return eraseCookie(tokenKey); }

/* 
jwt - token itself
expireDate - ISO8601 DateTime expiration as string
*/
export function setToken( jwt, expireDate ) {
    var d = new Date(expireDate);
    let expires = `expires=${d.toUTCString()}`;
    document.cookie = `${tokenKey}=${jwt};${expires};Same-Site=Strict;path=/`
}

/*
cname - cookie name
*/
export function getCookie(cname) {
    let name = `${cname}=`;
    
    let ca = document.cookie.split(';');
    
    for (let i = 0; i < ca.length; ++i) {
        
        let c = ca[i].trimStart();
        
        // trim function
        // while (c.charAt(0) == ' ') {
        //     c = c.substring(1);
        // }

        if ( c.indexOf(name) == 0) {
            return c.substring(name.length, c.length);
        }
    }

    return '';
}

/*
cname - cookie name
*/
export function eraseCookie(cname) {
    document.cookie = cname + '=; Path=/; Expires=Thu, 01 Jan 1970 00:00:01 GMT;';
}

function checkCookie() {
    let user = getCookie("username");
    if (user != "") {
        alert("Welcome again " + user);
    } else {
        user = prompt("Please enter your name:", "");
        if (user != "" && user != null) {
            setCookie("username", user, 365);
        }
    }
}