async function FetchToBuffer(url) {
    var buffer = await fetch(url)
        .then(response => response.arrayBuffer());
    var ptr = Module._malloc(buffer.byteLength);
    var responseBuffer = new Uint8Array(buffer);
    var managedBuffer = PtrToUint8Array(ptr, buffer.byteLength);
    for (i = 0; i < responseBuffer.length; ++i) {
        managedBuffer[i] = responseBuffer[i];
    }
    return ptr + "," + buffer.byteLength;
}

function Free(ptr) {
    Module._free(ptr);   
}

function PtrToUint8Array(ptr, size) {
    return new Uint8Array(Module.HEAPU8.buffer, ptr, size);
}