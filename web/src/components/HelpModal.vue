<script setup>

import { Modal as bsModal } from 'bootstrap'

let props = defineProps( { title:{ type: String, default: 'Help' }, msg:{ type: String, default: 'Help' } } );

// make it close on enter
function doEnterKey(event) {
    document.getElementById("okButton").click();
}

/* cancel */
function doClose(event)
{
    bsModal.getInstance( document.getElementById('helpModal') ).hide();
    //emit('doClose', true);
}

</script>

<template>

<div class="modal modal fade" id="helpModal" tabindex="-1" aria-hidden="true" @keydown.enter.prevent="doEnterKey">
    <div class="modal-dialog">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title" id="alertHeaderLabel">{{ props.title }}</h5>
            </div>
            <div class="modal-body">
                <div class="container-fluid">
                    <p>{{ props.msg }}</p>
                </div>
            </div>
            <div class="modal-footer">
                <button id="okButton" type="button" class="btn btn-primary" @click.stop="doClose">OK</button>
            </div>
        </div>
    </div>
</div>

</template>
