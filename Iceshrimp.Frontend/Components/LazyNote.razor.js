export function getScrollHeight(id) {

    const el = document.getElementById(id)
    if (el == null) {
        return null
    } else {
        return el.scrollHeight;

    }
}