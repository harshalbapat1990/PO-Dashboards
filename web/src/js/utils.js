/* set a common logger */
export const log = (msg) => { console.log(msg); }

/* test a string is empty or whitespace only */
export function isEmpty(s) {
    return s === null || s === undefined ? true : /^[\s\xa0]*$/.test(s);
}

/* test an array of strings for ANY empty strings */
export function isAnyEmpty(ary) {

    for (let i = 0; i < ary.length; ++i) {
        if (isEmpty(ary[i])) return true;
    }
    return false;
}

/**
 *  Formats the specified date as a string in the format 2024-10-15 to local time
 *  @param {DateObject} dateObj A Javascript date object
 *  @returns {String} A date string in the format of YYYY-DD-MM
 */
export const localAsStrDay = (dateObj) => dateObj.toLocaleString('en-CA', { year: 'numeric', month: '2-digit', day: '2-digit' });

/**
 *  Formats a ISO8601 datetime string to a date object and returns the full local date time in US format
 *  @param {ISO8601 String} date as an ISO8601 String
 *  @returns {String} A full US date string in local time
 */
export const dateAsLocal = (date) => !isEmpty(date) ? new Date(date).toLocaleString('en-US') : '';

/* test for a valid jacobs email address */
export function isJacobsEmail(s) {
    return s === null || s === undefined ? false : /^[a-zA-Z0-9._+-]{3,64}@jacobs.com$/i.test(s);
}

/* test for a web site address */
export function isURL(s) {
    return s === null || s === undefined ? false : /^(https?:\/\/)?([\w-]+\.)+[\w-]+(\/[\w-._~:/?#\[\]@!$&'()*+,;=%]*)?$/i.test(s);
}

/* test for a valid jacobs email address */
export function truncate(s, max = 128) {
    if ( s === null || s === undefined )
        return '';

    if ( max < 1 ) max = 1;

    if (s.length < max + 1) return s;

    return s.slice(0,max);
}

/* clamp inclusive */
export const clamp = (num, min, max) => !num || isNaN(num) ? min : Math.min(Math.max(num, min), max);

/* concats an array of strings back to a single string */
function array2text(ary) {
	return !ary ||  ary.length < 1 ? '' : ary.join('<br/>');
}