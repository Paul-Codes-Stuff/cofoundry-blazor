// The browser's IANA time zone, e.g. "Europe/London", for the [DateLocal] and [DateAndTimeLocal] fields.
export function getTimeZone() {
    return Intl.DateTimeFormat().resolvedOptions().timeZone;
}
