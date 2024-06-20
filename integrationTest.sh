#!/bin/bash

function build_devcontainer() {
    devcontainer up --workspace-folder ${PWD} --workspace-mount-consistency cached --id-label devcontainer.local_folder=${PWD} --default-user-env-probe loginInteractiveShell --build-no-cache --remove-existing-container --mount type=volume,source=vscode,target=/vscode,external=true --update-remote-user-uid-default on --mount-workspace-git-root true
    echo $?

    # Wait for pods to be ready
    output=$(k3s kubectl get pods -A -o jsonpath='{range .items[*]}{.status.phase}{"\n"}{end}')

    # Check if the output only contains "Succeeded" and "Running"
    while read -r line; do
    if [[ "$line" != "Succeeded" && "$line" != "Running" ]]; then
        echo "Not all pods are 'Succeeded' or 'Running'..."
        sleep 5
        k3s kubectl get pods -A
        output=$(k3s kubectl get pods -A -o jsonpath='{range .items[*]}{.status.phase}{"\n"}{end}')
        continue 2
    fi
    done <<< "$output"
    k3s kubectl get pods -A
}


function build_projects(){
    app_name=spacesdk-core

    # Build framework-core
    echo "Running:  docker exec ${app_name} bash -c \"dotnet build /workspaces/app_name/src\""
    docker exec ${app_name} bash -c "dotnet build /workspaces/${app_name}/src"
    echo $?

    # Build integrationTestHostPlugin
    echo "Running:  docker exec ${app_name} bash -c \"dotnet build /workspaces/${app_name}/test/integrationTestHostPlugin\""
    docker exec ${app_name} bash -c "dotnet build /workspaces/${app_name}/test/integrationTestHostPlugin"
    echo $?

    # Build integrationTestHost
    echo "Running:  docker exec ${app_name} bash -c \"dotnet build /workspaces/${app_name}/test/integrationTestHost\""
    docker exec ${app_name} bash -c "dotnet build /workspaces/${app_name}/test/integrationTestHost"
    echo $?

    # Build integrationTests
    echo "Running:  docker exec ${app_name} bash -c \"dotnet build /workspaces/${app_name}/test/integrationTests\""
    docker exec ${app_name} bash -c "dotnet build /workspaces/${app_name}/test/integrationTests"
    echo $?
}

function run_integration_tests(){

    app_name=spacesdk-core
    echo "environment variables:"
    printenv

    echo ""
    echo "Running: k3s kubectl exec -n payload-app deploy/${app_name} -- bash -c \"/usr/bin/dotnet  /workspaces/${app_name}/test/integrationTestHost/bin/Debug/net6.0/integrationTestHost.dll\" &"
    k3s kubectl exec -n payload-app deploy/${app_name} -- bash -c "/usr/bin/dotnet  /workspaces/${app_name}/test/integrationTestHost/bin/Debug/net6.0/integrationTestHost.dll" &
    host_pid=($!)

    echo "Running k3s kubectl exec -n payload-app deploy/${app_name}-client -- bash -c \"/usr/bin/dotnet test --verbosity detailed /workspaces/${app_name}/test/integrationTests/bin/Debug/net6.0/integrationTests.dll --logger \\"junit;LogFileName=/var/spacedev/tmp/test-results.xml\\"\" &"
    k3s kubectl exec -n payload-app deploy/${app_name}-client -- bash -c "/usr/bin/dotnet test --verbosity detailed /workspaces/${app_name}/test/integrationTests/bin/Debug/net6.0/integrationTests.dll --logger \"junit;LogFileName=/workspaces/${app_name}/.git/test-results.xml\"" &
    client_pid=($!)

    local return_code
    wait "$client_pid"
    return_code=$?
    if [[ $return_code -gt 0 ]]; then
        echo "Integration Tests Failed with return code: $return_code"
    fi
    kill "$host_pid"

    mv .git/test-results.xml ${PWD}
}


function main(){
    #build_devcontainer
    build_projects
    run_integration_tests
}

main