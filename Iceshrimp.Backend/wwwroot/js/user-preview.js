function navigate(event) {
    const target = event.target.getAttribute('data-target')
    if (event.ctrlKey || event.metaKey)
        window.open(target, '_blank');
    else
        window.location.href = target;
}