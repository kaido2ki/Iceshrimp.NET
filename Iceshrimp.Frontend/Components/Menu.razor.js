export function getPosition(ref){
    let rect = ref.getBoundingClientRect()
    let x = rect.right - (rect.width / 2) + window.scrollX;
    let y = rect.bottom + window.scrollY;
    return [x, y]
}