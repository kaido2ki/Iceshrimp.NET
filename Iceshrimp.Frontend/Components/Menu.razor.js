export function getPosition(ref, scrollY){
    let rect = ref.getBoundingClientRect()
    let x = rect.right - (rect.width / 2) + window.scrollX;
    let y = scrollY ? rect.bottom + window.scrollY : rect.bottom;
    return [x, y]
}