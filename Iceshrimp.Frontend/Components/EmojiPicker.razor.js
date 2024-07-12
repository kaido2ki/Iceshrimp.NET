export function getPosition(ref){
    let rect = ref.getBoundingClientRect()
    let x = rect.x + window.scrollX;  
    let y = rect.y + window.scrollY;
    return [x, y]
}

export function openDialog(ref){
    ref.show();
}

export function closeDialog(ref){
    ref.close();
}