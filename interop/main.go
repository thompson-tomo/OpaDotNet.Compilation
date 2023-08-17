package main

import (
	/*
		#include <stdlib.h>
		#include <string.h>

		struct OpaVersion {
		    char* libVersion;
		    char* goVersion;
		    char* commit;
		    char* platform;
		};

		struct OpaBuildParams {
		    char* target;
		    char* capabilitiesFile;
		    char* capabilitiesVersion;
		    int bundleMode;
		    char** entrypoints;
		    int entrypointsLen;
			int debug;
		};

		struct OpaFsBuildParams {
			char* source;
			struct OpaBuildParams params;
		};

		struct OpaBuildResult {
		    unsigned char* result;
		    int resultLen;
		    char* errors;
			char* log;
		};

	*/
	"C"
	"bytes"
	"context"
	"fmt"
	"github.com/open-policy-agent/opa/ast"
	"github.com/open-policy-agent/opa/compile"
	"github.com/open-policy-agent/opa/logging"
	"github.com/open-policy-agent/opa/version"
	"io"
	"unsafe"
)

func main() {
}

var opaVersion *C.struct_OpaVersion
var defaultCaps *ast.Capabilities

func init() {
	// We will be leaking this memory, but it is initialized only once so it should not be a big deal.
	opaVersion = (*C.struct_OpaVersion)(C.malloc(C.sizeof_struct_OpaVersion))
	C.memset(unsafe.Pointer(opaVersion), 0, C.sizeof_struct_OpaVersion)

	(*opaVersion).libVersion = C.CString(version.Version)
	(*opaVersion).goVersion = C.CString(version.GoVersion)
	(*opaVersion).commit = C.CString(version.Vcs)
	(*opaVersion).platform = C.CString(version.Platform)

	defaultCaps = ast.CapabilitiesForThisVersion()
}

type buildParams struct {
	source              string
	capabilitiesFile    string
	capabilitiesVersion string
	target              string
	bundleMode          bool
	entrypoints         []string
	debug               bool
}

//export OpaGetVersion
func OpaGetVersion() *C.struct_OpaVersion {
	return opaVersion
}

//export OpaBuildFromFs
func OpaBuildFromFs(fsParams *C.struct_OpaFsBuildParams, buildResult **C.struct_OpaBuildResult) int {
	var logger logging.Logger
	loggerBuffer := bytes.NewBuffer(nil)

	if fsParams.params.debug == 0 {
		logger = logging.NewNoOpLogger()
	} else {
		sl := logging.New()
		sl.SetLevel(logging.Debug)
		sl.SetOutput(loggerBuffer)
		logger = sl
	}

	eps := make([]string, 0, fsParams.params.entrypointsLen)

	if fsParams.params.entrypoints != nil {
		var pEps **C.char = fsParams.params.entrypoints

		for _, entrypoint := range unsafe.Slice(pEps, int(fsParams.params.entrypointsLen)) {
			ep := C.GoString(entrypoint)

			if len(ep) > 0 {
				eps = append(eps, ep)
			}
		}
	}

	bp := &buildParams{
		source:              C.GoString(fsParams.source),
		capabilitiesFile:    C.GoString(fsParams.params.capabilitiesFile),
		capabilitiesVersion: C.GoString(fsParams.params.capabilitiesVersion),
		target:              C.GoString(fsParams.params.target),
		bundleMode:          fsParams.params.bundleMode > 0,
		entrypoints:         eps,
		debug:               fsParams.params.debug > 0,
	}

	logger.Debug("Compiler version: %s", version.Version)
	logger.Debug("Explicit %d entrypoints: %v", len(eps), eps)

	resultBytes, err := opaBuild(bp, loggerBuffer)

	*buildResult = (*C.struct_OpaBuildResult)(C.malloc(C.sizeof_struct_OpaBuildResult))
	C.memset(unsafe.Pointer(*buildResult), 0, C.sizeof_struct_OpaBuildResult)

	logger.Debug("Result pointer: %p", *buildResult)

	if bp.debug {
		(**buildResult).log = C.CString(loggerBuffer.String())
	}

	if err != nil {
		(**buildResult).errors = C.CString(err.Error())
		return -1
	}

	bts := resultBytes.Bytes()

	(**buildResult).resultLen = C.int(len(bts))
	(**buildResult).result = (*C.uchar)(C.CBytes(bts))

	return 0
}

//export OpaFree
func OpaFree(ptr *C.struct_OpaBuildResult) {
	if ptr == nil {
		return
	}

	C.free(unsafe.Pointer((*ptr).result))
	C.free(unsafe.Pointer((*ptr).errors))
	C.free(unsafe.Pointer((*ptr).log))
	C.free(unsafe.Pointer(ptr))
}

func opaGetCaps(pathOrVersion string, isFile bool) (*ast.Capabilities, error) {
	var result *ast.Capabilities
	var errPath, errVersion error

	if isFile {
		result, errPath = ast.LoadCapabilitiesFile(pathOrVersion)
	} else {
		result, errVersion = ast.LoadCapabilitiesVersion(pathOrVersion)
	}

	if errVersion != nil || errPath != nil {
		return nil, fmt.Errorf("no such file or capabilities version found: %v", pathOrVersion)
	}

	return result, nil
}

func opaMergeCaps(a *ast.Capabilities, b *ast.Capabilities) *ast.Capabilities {
	result := &ast.Capabilities{}
	result.Builtins = append(a.Builtins, b.Builtins...)
	result.Features = append(a.Features, b.Features...)
	result.AllowNet = append(a.AllowNet, b.AllowNet...)
	result.FutureKeywords = append(a.FutureKeywords, b.FutureKeywords...)
	result.WasmABIVersions = append(a.WasmABIVersions, b.WasmABIVersions...)
	return result
}

func opaBuild(params *buildParams, loggerBuffer io.Writer) (*bytes.Buffer, error) {
	buf := bytes.NewBuffer(nil)

	var fileCaps *ast.Capabilities = nil
	var verCaps *ast.Capabilities = nil
	var caps *ast.Capabilities
	var capsErr error

	if len(params.capabilitiesFile) > 0 {
		fileCaps, capsErr = opaGetCaps(params.capabilitiesFile, true)
		if capsErr != nil {
			return nil, capsErr
		}
	}

	if len(params.capabilitiesVersion) > 0 {
		verCaps, capsErr = opaGetCaps(params.capabilitiesVersion, false)
		if capsErr != nil {
			return nil, capsErr
		}
	}

	if fileCaps == nil && verCaps == nil {
		caps = defaultCaps
	} else {
		if fileCaps == nil {
			caps = verCaps
		} else if verCaps == nil {
			caps = fileCaps
		} else {
			caps = opaMergeCaps(fileCaps, verCaps)
		}
	}

	compiler := compile.New().
		WithTarget(params.target).
		WithAsBundle(params.bundleMode).
		WithEntrypoints(params.entrypoints...).
		WithPaths(params.source).
		WithCapabilities(caps).
		WithEnablePrintStatements(true).
		WithOutput(buf).
		WithRegoAnnotationEntrypoints(true)

	if params.debug {
		compiler.WithDebug(loggerBuffer)
	}

	err := compiler.Build(context.Background())

	if err != nil {
		return nil, err
	}

	return buf, nil
}
