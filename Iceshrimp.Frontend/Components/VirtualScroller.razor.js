export function GetHeight(ref){
    if (ref != null) {
        console.log(ref.scrollHeight)
        return ref.scrollHeight;
    } else {
        return 0;
    }
}