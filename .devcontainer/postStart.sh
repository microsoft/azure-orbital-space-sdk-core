
# Pull in the app.env file built by the feature
source "/spacefx-dev/app.env"
source "${SPACEFX_DIR:?}/modules/load_modules.sh" $@ --log_dir "${SPACEFX_DIR:?}/logs/${APP_NAME:?}"

############################################################
# Download the protos used by dapr
############################################################
function download_dapr_protos(){
    info_log "START: ${FUNCNAME[0]}"

    local dapr_protos=(
        "sentry/v1/sentry.proto"
        "common/v1/common.proto"
        "internals/v1/service_invocation.proto"
        "internals/v1/apiversion.proto"
        "internals/v1/status.proto"
        "operator/v1/operator.proto"
        "components/v1/common.proto"
        "components/v1/bindings.proto"
        "components/v1/state.proto"
        "components/v1/pubsub.proto"
        "placement/v1/placement.proto"
        "runtime/v1/dapr.proto"
        "runtime/v1/appcallback.proto"
    )

    for dapr_proto in "${dapr_protos[@]}"; do
        if [[ ! -f "${SPACEFX_DIR}/protos/dapr/proto/${dapr_proto}" ]]; then
            debug_log "Missing '${SPACEFX_DIR}/protos/dapr/proto/${dapr_proto}' - triggering download"
            missing_protos=true
            break
        fi
    done

    if [[ "${missing_protos}" == false ]]; then
        info_log "All dapr protos found.  Nothing to do."
        return
        info_log "END: ${FUNCNAME[0]}"
    fi

    info_log "Missing dapr protos.  Starting download..."


    trace_log "Calculating dapr version..."
    run_a_script "jq -r '.config.charts[] | select(.group == \"dapr\") | .version' < ${SPACEFX_DIR}/tmp/config/spacefx-config.json" DAPR_VER
    trace_log "Dapr version calculated: ${DAPR_VER:?}"

    tmp_filename="${SPACEFX_DIR}/tmp/dapr-${DAPR_VER}.tar.gz"
    download_uri="https://codeload.github.com/dapr/dapr/tar.gz/refs/tags/v${DAPR_VER}"

    run_a_script "curl --silent --create-dirs --output ${tmp_filename} -L ${download_uri}"

    debug_log "...dapr protos downloaded.  Starting extraction..."

    create_directory "${SPACEFX_DIR}/protos/dapr/proto"
    run_a_script "tar -xf ${tmp_filename} --directory ${SPACEFX_DIR}/protos/dapr/proto --wildcards \"*/dapr/**/*.proto\" --strip-components=3"

    debug_log "...removing temp file..."
    run_a_script "rm ${tmp_filename}"

    info_log "Successfully downloaded Dapr protos"


    info_log "END: ${FUNCNAME[0]}"
}

download_dapr_protos