.DEFAULT_GOAL     = help

WORKLOAD_PROJECT  = Iceshrimp.Frontend
BUILD_PROJECT     = Iceshrimp.Backend
CONFIGURATION     = Release

DOTNET_CMD        = dotnet
VERBOSE           = false
DEP_VULN_WERROR   = false

AOT               = false
VIPS              = false
BUNDLE_NATIVE     = false

RELEASE_TARGETS   = linux-glibc-amd64 linux-glibc-arm64 linux-musl-amd64 linux-musl-arm64

ifeq (${VERBOSE},false)
	TL_ENV        = MSBUILDTERMINALLOGGER=true
else
	TL_ENV        = MSBUILDTERMINALLOGGER=false
endif

PUBLISH_CMD       = ${TL_ENV} ${DOTNET_CMD} publish ${BUILD_PROJECT} -c ${CONFIGURATION} -noLogo
BUILD_CMD         = ${TL_ENV} ${DOTNET_CMD} build -noLogo
TEST_CMD          = ${TL_ENV} ${DOTNET_CMD} test --no-build --nologo

BUILD_FLAGS       = -p:EnableLibVips=${VIPS} -p:BundleNativeDeps=${BUNDLE_NATIVE} -p:DependencyVulnsAsError=${DEP_VULN_WERROR}
PUBLISH_FLAGS     = -p:EnableAOT=${AOT} -p:DeterministicSourcePaths=true -p:ContinuousIntegrationBuild=true ${BUILD_FLAGS}
PUBLISH_FLAGS_EXT = ${PUBLISH_RIDARG} -o publish/${TARGETRID} ${PUBLISH_FLAGS}
RELEASE_FLAGS     = -r ${TARGETRID} -o release/${TARGETPLATFORM} ${PUBLISH_FLAGS}

TARGETRID         = $(TARGETPLATFORM:linux-glibc-%=linux-%)
PUBLISH_RIDARG    = $(if $(TARGETRID),-r $(TARGETRID),)
ARCHIVE_TGTNAME   = $(patsubst linux-glibc-%,linux-%-glibc,$(patsubst linux-musl-%,linux-%-musl,$(TARGETPLATFORM)))
ARCHIVE_BASENAME  = iceshrimp.net
ARCHIVE_VERSION   = unknown
ARCHIVE_DIRNAME   = ${ARCHIVE_BASENAME}-${ARCHIVE_VERSION}-${ARCHIVE_TGTNAME}
ARCHIVE_FILENAME  = ${ARCHIVE_DIRNAME}.tar.zst
ARTIFACT_DIR      = artifacts
ARTIFACT_CMD      = tar caf ${ARTIFACT_DIR}/${ARCHIVE_FILENAME} --transform 's,^release/${TARGETPLATFORM},${ARCHIVE_DIRNAME},' release/${TARGETPLATFORM}

.PHONY            : --release-pre --release-post --workload-restore --release-% artifact-% publish build test publish-aot release-artifacts help

--release-pre:
	@echo 'Building release artifacts for targets: ${RELEASE_TARGETS}'
	@echo 'This will take a while.'
	@echo
	@rm -rf release artifacts

--release-post:
	@echo 'Finished building release artifacts.'

--workload-restore:
	@if ! dotnet workload list | grep -q '^wasm-tools\s'; then echo "Missing wasm-tools workload. Please run \`dotnet workload install wasm-tools\` before running this target."; exit 1; fi

--release-%: TARGETPLATFORM=$*
--release-%:
	@echo 'Building for release with flags: ${RELEASE_FLAGS}'
	@${PUBLISH_CMD} ${RELEASE_FLAGS}

--artifact-%: TARGETPLATFORM=$*
--artifact-%:
	@echo -n 'Generating artifact ${ARCHIVE_FILENAME}...'
	@mkdir -p ${ARTIFACT_DIR}
	@${ARTIFACT_CMD}
	@echo " Done!"
	@echo

publish:
	@echo 'Building for publish with flags: ${strip ${PUBLISH_FLAGS_EXT}}'
	@${PUBLISH_CMD} ${PUBLISH_FLAGS_EXT}

build:
	@echo 'Building with flags: ${BUILD_FLAGS}'
	@${BUILD_CMD} ${BUILD_FLAGS}

test: build
	@echo
	@${TEST_CMD}

clean:
	@git clean -xfd -e '*.DotSettings.user' -e '/.idea/**' -e 'configuration*.ini' -e '.gitignore' -e '/.vs/**'

cleanall:
	@git clean -xfd

publish-aot: AOT=true
publish-aot: --workload-restore publish

release-artifacts: AOT=true
release-artifacts: VIPS=true
release-artifacts: BUNDLE_NATIVE=true
release-artifacts: --release-pre $(foreach tgt,$(RELEASE_TARGETS),--release-$(tgt) --artifact-$(tgt)) --release-post

help:
	@echo '-- Makefile --'
	@echo 'Targets:'
	@echo '  test               - runs all available unit tests'
	@echo '  build              - compiles the application (for development)'
	@echo '  clean              - removes all build-related files from the working tree'
	@echo '  cleanall           - removes all untracked files from the working tree'
	@echo '  publish            - compiles the application (for production)'
	@echo '  publish-aot        - equivalent to `make AOT=true publish`'
	@echo '  release-artifacts  - generates release artifacts for all supported architectures'
	@echo
	@echo 'Common build arguments:'
	@echo '  AOT=<boolean>'
	@echo '    Enables AOT compilation for the WASM frontend, trading increased compile time'
	@echo '    for increased frontend performance. This option is only effective during publish.'
	@echo '  VIPS=<boolean>'
	@echo '    Enables LibVips support, either requires the system to have libvips installed,'
	@echo '    or BUNDLE_NATIVE to be set.'
	@echo '  BUNDLE_NATIVE=<boolean>'
	@echo '    Bundles native dependencies in the output directory'
	@echo '  TARGETRID=<rid>'
	@echo '    Sets the target runtime identifier. This option is only effective during publish.'
	@echo 'Miscellaneous build arguments:'
	@echo '  VERBOSE=<boolean>'
	@echo '    Disables beautified build output'
	@echo '  DOTNET_CMD=<path>'
	@echo '    Path to the `dotnet` binary to call'
	@echo '  CONFIGURATION=<Release|Debug>'
	@echo '    Configuration to build in, defaults to `Release`.'
	@echo
	@echo 'For example, if you want to run target `build` with VIPS enabled: `make VIPS=true build`'
	@echo 'For production deployments, you likely want to call `make publish-aot`.'
