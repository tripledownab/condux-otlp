# CLAUDE.md

Guidance for AI agents and contributors working in this repository. `README.md` is written for someone
consuming the package; this is written for someone changing it.

## What this is

A reader for OpenTelemetry Protocol **logs** payloads, in both encodings the protocol defines, with no
package dependencies. `src/Condux.Otlp` decodes the wire format and nothing else: no transport, no
semantic conventions, no opinion about how a value should be shown. That restraint is the product, so a
change that adds policy to the library needs an argument, not just a use case.

## This repository is public

Everything here is read by strangers, and a push cannot be recalled. Write for someone who has never
seen the maintainer's other work: no names from it, no local paths, no process that only means something
elsewhere. **Fixtures count** — they are the usual way a stray name travels, so the ones here use
synthetic data and the capture tool rewrites source paths so a stack trace cannot carry the machine it
was captured on.

Commit messages are as public and as permanent as the tree, which is why `.github/scan/scan.sh` reads
both. Run it before pushing, not after.

## Commands

```bash
dotnet test Condux.Otlp.slnx -c Release          # all three targets; needs the .NET 8, 9 and 10 runtimes
dotnet test Condux.Otlp.slnx -c Release -f net10.0
dotnet format Condux.Otlp.slnx --verify-no-changes
bash .github/scan/scan.sh                        # forbidden strings, tracked files and commit messages
bash tools/capture-fixtures.sh                   # recapture the fixtures; needs Docker and Node
```

CI runs build, test and format in one job, the scan in another (over the commit messages as well as the
tree), and a pack in a third that fails if the built package declares a dependency. That last check is
the package's one claim made enforceable; do not weaken it to land a change.

## Layout

```
src/Condux.Otlp/
  OtlpLogs.cs          the two entry points; OtlpReadResult.cs is what they return
  Limits.cs            the decoding bounds, and why each is the number it is
  Messages/            the protocol's types, one file per group, plus the response encoder
  Protobuf/            ProtobufReader (primitives) -> MessageDecoder (the loop) -> the per-message decoders
  Json/                JsonScalars (values) -> JsonFields (members) -> the per-message decoders
tests/                 fixtures captured from real exporters, guard tests, an every-field test
tools/                 the fixture capture harness; not part of the package
proto-reference/       unmodified .proto files at the release this implements, for checking field numbers
```

`MessageDecoder` holds the wire loop that every protobuf message decoder runs. It exists so two rules are
stated once rather than eight times: how deep nesting may go, and that an unrecognised field is stepped
over by its wire type. Adding a message means writing a `FieldReader`, not another loop.

## Code shape (checked while writing, not at review)

1. **No file over 200 lines**, excluding tests, and every file opens with a comment saying what belongs
   in it. Without that line a split is just a table of contents.
2. **Hoist constants to the top of the file**, named, with a comment saying why the value is what it is.
   A field number or a mask buried at the foot of a class is one nobody can change safely.
3. **Functions start with a verb. A conversion is `To…`, never `From…`.**
4. **Name a file for its category, not its first function**, so the second one fits without a rename.
5. **No fallback that masks an invariant.** `?? ""` on a value the type system proves non-null tells the
   next reader that null happens. `git grep '??' -- src` returns nothing; keep it that way.
6. **Nothing without a caller today.** No option, flag, generic parameter or abstraction on speculation.
   `OtlpReadResult` was generic over one type argument for a signal that is not implemented, and
   `AnyValue.ToString` rendered composites as JSON, which is a formatting opinion. Both are gone.

## Verify mechanically, not by eye

A checklist item you judge finds one instance; the same item written as a command finds all of them.

- **Mutation over eyeballing.** Break each field's read path in turn and confirm a test dies. Two value
  kinds decoded to nothing with the whole suite green until a 53-mutation sweep found them.
- **A test can pass for the wrong reason.** A nesting test once passed because its innermost bytes were
  malformed, not because the depth limit worked. Ask what the payload would do if the guard did nothing.
- **Run every gate against a planted failure and a clean tree.** Both gates here once returned success
  over inputs they never opened.
- **Compile the code samples in the README.** The first one did not compile for three commits.

## Things that will catch you out

- **Trace and span ids are hex in the JSON encoding, not base64.** This is the one place OTLP departs
  from the standard protobuf JSON mapping, and a plausible id is valid as both, so the mistake decodes
  to different bytes rather than failing. The two collector fixtures are one export in both encodings and
  the test that compares them is what pins it.
- **The JSON encoding is stricter than proto3-JSON, not looser.** Member names are lowerCamelCase only,
  and enum values must be integers; the spec spells both out. Confirmed against two independent producers
  before the decoder was made strict, because a stricter reading than reality rejects whole exports.
- **Compression is the transport's business.** Producers disagree: the collector gzips by default, the
  .NET and JS SDK exporters do not. The capture harness undoes it, and a receiver must honour
  `Content-Encoding` before the payload reaches this package.
- **Fixtures must come from real exporters.** A decoder checked against its own writer shares its
  assumptions and cannot disprove them. Three implementations produced the ones here.
- **A real exporter only emits what it has**, so the fixtures cannot cover a field left at its default.
  `EveryFieldTests` exists for exactly that gap; extend it when the protocol gains a field.
- **Stack traces and machine paths.** The capture tool rewrites source paths so a fixture is identical
  wherever it is captured. Never fix a leaked path in review; fix it at the source.

## Moving to a later protocol release

Replace `proto-reference/`, reconcile every field number against it, then recapture the fixtures.
Upstream adds fields and does not renumber them, so the usual outcome is new fields to model. The release
this targets is named in `README.md`.
