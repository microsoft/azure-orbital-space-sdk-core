# Azure Orbital Space SDK - Core

[![spacefx-dev-build-publish](https://github.com/microsoft/azure-orbital-space-sdk-core/actions/workflows/devcontainer-feature-build-publish.yml/badge.svg)](https://github.com/microsoft/azure-orbital-space-sdk-core/actions/workflows/devcontainer-feature-build-publish.yml)

This repository hosts a common code base for all host services, payload apps, platform services.  It scaffolds all message publishing and subscribing used by all services and payload apps.

Outputs:

| Item                                        | Description                                                                                                                                                 |
| ------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Microsoft.Azure.SpaceSDK.Core.1.0.0.nupkg` | DotNet Nuget Package to be used as a base reference package for all other components in the Space Framework                                                 |
| `Common.proto`                              | A protobuf file to be referenced by all components leveraging the SpaceSDK-Core.  Common statuses, file upload/download, and other object types are stored. |

## Building

1. Provision /var/spacedev
    ```bash
    # clone the azure-orbital-space-sdk-setup repo and provision /var/spacedev
    git clone https://github.com/microsoft/azure-orbital-space-sdk-setup
    cd azure-orbital-space-sdk-setup
    bash ./.vscode/copy_to_spacedev.sh
    cd -
    ```

1. Trigger build of Azure Orbital Space SDK Nuget Package
    ```bash
    # clone this repo
    git clone https://github.com/microsoft/azure-orbital-space-sdk-core

    cd azure-orbital-space-sdk-setup

    # Trigger the build_app.sh from azure-orbital-space-sdk-setup
    /var/spacedev/build/dotnet/build_app.sh \
        --annotation-config azure-orbital-space-sdk-core.yaml \
        --architecture amd64 \
        --app-project src/spacesdk-core.csproj \
        --app-version 0.11.0 \
        --output /var/spacedev/tmp/spacesdk-core/output \
        --repo-dir ${PWD} \
        --nuget-project src/spacesdk-core.csproj \
        --no-container-build
    ```

1. Copy the build artifacts to their locations in /var/spacedev
    ```bash
    sudo mkdir -p /var/spacedev/nuget/core
    sudo mkdir -p /var/spacedev/protos/spacefx/protos/common

    sudo cp /var/spacedev/tmp/spacesdk-core/output/amd64/Microsoft.Azure.SpaceSDK.Core.0.11.0.nupkg /var/spacedev/nuget/core/
    sudo cp ${PWD}/src/Protos/Common.proto /var/spacedev/protos/spacefx/protos/common/
    ```

1. Push the artifacts to the container registry
    ```bash
    # Push the nuget package to the container registry
    /var/spacedev/build/push_build_artifact.sh --artifact /var/spacedev/nuget/core/Microsoft.Azure.SpaceSDK.Core.0.11.0.nupkg --annotation-config azure-orbital-space-sdk-core.yaml --architecture amd64 --artifact-version 0.11.0

    # Push the Common.proto to the container registry
    /var/spacedev/build/push_build_artifact.sh --artifact /var/spacedev/protos/spacefx/protos/common/Common.proto --annotation-config azure-orbital-space-sdk-core.yaml --architecture amd64 --artifact-version 0.11.0
    ```

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft
trademarks or logos is subject to and must follow
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
