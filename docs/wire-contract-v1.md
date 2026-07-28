---
name: wire-contract-v1
description: The FoodUs relay wire contract, major version 1 - transport, authentication, data types, envelope schema, endpoints, error semantics, retention, and evolution rules. The single source of truth both the relay and the FoodUs app implement.
metadata:
  version: "1.0"
  contract_major_version: 1
  contract_minor_version: 0
  owner: "Jarryd Adaens"
  repo: "FoodUs-Server"
---
# FoodUs Relay - Wire Contract v1

## 1. Purpose and Authority

This document is the wire contract between the FoodUs relay and its clients. It is the single
source of truth: the relay in this repository and the FoodUs Android app are two independent
implementations of this one document, and neither owns the contract by virtue of shipping first.

- This repository owns the contract. The app conforms to it.
- Contract changes are proposed by report, adjudicated by the owner, and carried across the
  repository boundary by the owner. Agents never negotiate the contract directly.
- When the contract changes, both sides change in the same sitting.
- Anyone may stand up their own relay from this specification. The mechanism is public; the
  values (endpoint address, keys, credentials) are private and appear nowhere in this document.

Related context: [design.md](../context/design.md), [milestone-3.md](../context/milestones/milestone-3.md),
[laws.md](../context/laws.md).

All examples in this document use the placeholder host `https://relay.example`. No real host,
address, or credential appears here, and none may be added.

---

## 2. Constitutional Constraints

These are binding on every implementation of this contract. A relay that violates one of them
is not a conforming relay, regardless of whether it passes every other check.

1. **No accounts, no login, no sessions.** There is no registration of a person, no credential
   the user chooses or remembers, and no session token. Identity is a device-held key pair over
   a client-generated GUID, and every request stands alone (Section 5).
2. **No server-readable diary data.** The relay stores sealed ciphertext and never parses,
   inspects, transforms, or indexes an envelope payload.
3. **No server-side backup.** Each phone's local database is the sole source of truth. The relay
   is a transmission vector between databases, nothing more.
4. **No web or companion clients.** The contract exists to serve the phone app.
5. **Undelivered envelopes are swept after 30 days** (Section 10).
6. **Blocked requesters receive "user not found", byte-identical to the response for a
   nonexistent user** (Sections 8.2 and 9.3).
7. **HTTPS is mandatory.** Plain HTTP is disqualifying even for sealed envelopes. The relay
   process itself binds to localhost and is never internet-facing; a reverse proxy terminates
   TLS in front of it.
8. **The endpoint address is private.** It is never published in this repository, in this
   document, or in any client artifact. It is typed into the app by the operator.

---

## 3. Transport and Base Path

- **Scheme:** HTTPS only. A client that receives a plain-HTTP relay URL must refuse to use it.
- **Base path:** every contract route is prefixed with the contract major version: `/v1/`.
  A major version is a parallel route family, never an edit (Section 4).
- **Content type:** `application/json; charset=utf-8` for all request and response bodies,
  except where a body is absent. JSON is UTF-8 encoded and must not contain a byte order mark.
- **Methods:** only `GET` and `POST` are used in v1. Endpoints that carry a secret-ish value
  (friend codes) use `POST` with a body so the value never lands in a URL, proxy log, or
  browser-style history.
- **Binary encoding:** every binary value on the wire (public keys, signatures, hashes, nonces,
  ciphertext) is **base64url without padding** (RFC 4648 §5, `-` and `_`, no `=`).
- **Idempotency:** `POST /v1/profile`, `POST /v1/blocks`, and `POST /v1/messages/acknowledge`
  are idempotent. Repeating them with the same body is safe and returns the same shape.
- **Compression, keep-alive, HTTP version:** unconstrained. Clients must not depend on any of
  them.

Example request line used throughout this document:

```http
POST /v1/profile HTTP/1.1
Host: relay.example
```

---

## 4. Versioning and Evolution Rules

Three independent version numbers exist. Keeping them distinct is what lets the system evolve.

| Version | Where it lives | What it governs |
| --- | --- | --- |
| Contract major | The `/v1/` route prefix | The route family and the meaning of every field in it |
| Contract minor | `GET /v1/capabilities` | Additive amendments inside major 1 |
| Envelope version | `envelopeVersion` on every envelope | The payload contract between two phones |

### 4.1 Additive only within a major version

Inside `/v1/`, an amendment may **only add**: a new optional request field, a new response
field, a new endpoint, a new capability name, or a new envelope version. It may never rename,
remove, repurpose, narrow, or change the type or meaning of anything already specified. The
moment a rename or removal is genuinely needed, that is the signal to stand up `/v2/`, not to
edit v1.

Every additive amendment increments the **contract minor version** and appends an entry to the
Change Log (Section 11).

### 4.2 Two-way tolerance

Both sides deserialize tolerantly. This is what makes additive evolution safe.

- **Unknown fields are ignored.** A new relay may send fields an old app has never heard of; the
  app ignores them rather than failing.
- **Absent fields mean "not provided", never a crash.** A new app may talk to an old relay that
  omits a field it expects; the app treats it as absent and degrades.
- Tolerance applies to JSON objects only. It never applies to `envelopeVersion`, which is
  refused loudly when unknown (Section 4.4).

### 4.3 Major versions as parallel routes

A breaking change stands up `/v2/` **alongside** a still-running `/v1/`. Clients drift over at
their own pace. `/v1/` is decommissioned only after a cutover window of one to two months, and
the relay announces the coming removal through the capability endpoint before it happens.

### 4.4 Envelope versions and refuse-loudly

Every envelope carries `envelopeVersion` (Section 7). A receiver that sees a version it does not
know **refuses loudly** — it surfaces the refusal to the user rather than silently dropping or
half-parsing the packet.

- The **relay** refuses an unknown `envelopeVersion` at push time with
  `unsupported_envelope_version` (Section 9.4). It never stores an envelope whose version it
  does not list in `envelopeVersions`.
- The **app** may still meet an unknown version on drain — for example, a newer relay accepted a
  v2 envelope from an updated phone while this phone is still on v1. That case is governed by
  the leave-queued rule in Section 8.5.

### 4.5 Capability negotiation

Clients must not assume a capability exists. Before exposing a relay-backed feature, the client
calls `GET /v1/capabilities` (Section 8.7) and hides or greys anything the connected relay does
not report. **Server leads, app follows:** new relay capability deploys first and sits dormant
until an app consumes it.

---

## 5. Authentication

There are no accounts, so authentication proves exactly one thing: **the caller holds the
private key registered against a GUID.** Every authenticated request is proven independently
with a detached signature; there is no login step, no session, and no server-issued token.

> Adjudicated by the owner, 2026-07-28 (run `rails-boss-execute`): detached request signing with
> a timestamp-plus-nonce replay window, and re-key announcements trusted only when the new
> public key is signed by the old key.

### 5.1 Algorithms and encodings

Chosen so that Android Keystore (client) and the .NET built-in cryptography libraries (relay)
can both implement the scheme with stock primitives and no third-party dependency.

| Element | v1 value |
| --- | --- |
| Key type | EC, curve NIST P-256 (`secp256r1` / `prime256v1`) |
| Signature algorithm | ECDSA with SHA-256 (`SHA256withECDSA` on Android; `ECDsa` + `HashAlgorithmName.SHA256` on .NET) |
| Signature encoding | ASN.1 DER `SEQUENCE { r INTEGER, s INTEGER }` — the JCA default. .NET verifies this with `DSASignatureFormat.Rfc3279DerSequence`. |
| Public key encoding | X.509 `SubjectPublicKeyInfo` DER — `PublicKey.getEncoded()` on Android; `ImportSubjectPublicKeyInfo` on .NET |
| Body hash | SHA-256 over the exact request body bytes |
| Wire encoding of all of the above | base64url, unpadded |

The algorithm set is part of the contract, not a negotiation. A future algorithm is added as an
additive amendment with an explicit key-type field, never by silently accepting a second one.

### 5.2 Request headers

Every authenticated request carries all five headers. Missing, malformed, or duplicated headers
are `unauthorized` (Section 9.2).

| Header | Value |
| --- | --- |
| `X-FoodUs-Guid` | The caller's GUID, canonical lowercase form (Section 6.1) |
| `X-FoodUs-Timestamp` | Request time as RFC 3339 UTC with a `Z` suffix, second precision |
| `X-FoodUs-Nonce` | 16 random bytes, base64url unpadded (22 characters) |
| `X-FoodUs-Body-Sha256` | SHA-256 of the request body bytes, base64url unpadded. For a request with no body, the hash of the empty byte string. |
| `X-FoodUs-Signature` | The detached signature over the canonical string of Section 5.3 |

### 5.3 The canonical signing string

The signature covers method, path, body hash, timestamp, and nonce. The signed bytes are the
UTF-8 encoding of exactly six lines joined by a single LF (`\n`), with **no trailing newline**:

```text
FoodUs-Relay-Request-v1
<HTTP method, uppercase>
<request path including query string, exactly as sent, no host, no fragment>
<X-FoodUs-Body-Sha256 value>
<X-FoodUs-Timestamp value>
<X-FoodUs-Nonce value>
```

Line 1 is a fixed domain-separation prefix so a signature produced for this contract can never
be replayed as a signature for anything else. Worked example (line breaks are the literal LF
separators):

```text
FoodUs-Relay-Request-v1
POST
/v1/messages
47DEQpj8HBSa-_TImW-5JCeuQeRkm5NMpJWZG3hSuFU
2026-07-28T04:15:30Z
9Nn3aQ7cVb1sTxK0Lm2PqA
```

The relay recomputes this string from the request it actually received — never from a
client-supplied copy — and verifies `X-FoodUs-Signature` against the public key registered for
`X-FoodUs-Guid`. It must also confirm that `X-FoodUs-Body-Sha256` matches the body bytes it
read; a mismatch is `unauthorized`, not `bad_request`, because the signature no longer covers
the payload.

### 5.4 Replay protection

- **Freshness window:** the relay rejects any request whose `X-FoodUs-Timestamp` differs from
  server time by more than **120 seconds** in either direction. The symmetric window tolerates
  ordinary phone clock skew without granting a useful replay horizon.
- **Nonce cache:** the relay stores every accepted `(guid, nonce)` pair for at least
  **300 seconds** — the freshness window plus margin — and rejects a repeat within that period.
  Entries older than the retention period are discarded; the cache is bounded by the window, not
  by traffic history.
- Both failures return the same `unauthorized` response (Section 9.2). The relay does not tell a
  caller which check failed.
- Clients that see repeated `unauthorized` responses may read `serverTime` from
  `GET /v1/capabilities` (unauthenticated) to correct their clock offset. The relay never
  accepts a client-asserted offset.

### 5.5 First registration (trust on first use)

A GUID's very first `POST /v1/profile` has no key registered yet, so the request is **self-
signed**: it is signed with the private key whose public half the body registers, and the relay
verifies the signature against that body-supplied key.

- If the GUID is **unknown**, the relay verifies the self-signature, then binds the GUID to the
  key. This is trust on first use, and it is the only moment a key arrives unvouched.
- If the GUID is **already known**, the body-supplied key is ignored for verification. The
  request must verify against the **registered** key, and any key change must follow Section 5.6.
  This is what stops a known GUID from being overwritten, drained, or impersonated.

Because a GUID is a client-generated random UUID, an attacker cannot usefully squat one.

### 5.6 Re-key announcements

A device replacing its key pair announces the new key through `POST /v1/profile`:

1. The **request itself is signed with the old (currently registered) key** — proving the caller
   still controls the identity.
2. The body carries `publicKey` (the new key) and `previousKeySignature`: a signature made by
   the **old** key over the canonical re-key statement below.

```text
FoodUs-Relay-Rekey-v1
<guid>
<new publicKey, base64url>
```

Three lines, LF-joined, no trailing newline, UTF-8, signed and encoded exactly as Section 5.1
prescribes.

The relay accepts the new key only when both signatures verify. It rejects any profile update
that changes `publicKey` without a valid `previousKeySignature`, with `unauthorized`. A new key
never arrives on the authority of the new key alone.

**Key loss is terminal by design.** The private key lives and dies with the device; there is no
recovery path, no reset, and no operator override — any of those would be an account system. A
person who loses their key registers a new GUID and re-exchanges friend codes.

### 5.7 Unauthenticated endpoints

`GET /v1/capabilities` is the only unauthenticated route in v1. It exposes no user data.

---

## 6. Data Types

### 6.1 GUID

Client-generated random UUID (RFC 4122 version 4), transmitted in **canonical lowercase
hyphenated form**: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`. It is the permanent primary key for
everything social and never changes for the life of a profile. The relay treats it as an opaque
identifier, validates the format, and never mints one.

### 6.2 Username

Free text, **purely cosmetic**. Collisions are allowed and expected; the username is explicitly
not an identifier and is never used for lookup. Unicode NFC, 1 to 40 code points after trimming
leading and trailing whitespace, no control characters. The relay stores and echoes it and
attaches no meaning to it.

### 6.3 Public key

EC P-256 public key, X.509 SubjectPublicKeyInfo DER, base64url unpadded (Section 5.1). The relay
validates that the value parses as a P-256 SPKI key and rejects anything else with
`bad_request`.

### 6.4 Friend code

A revocable, disposable handle over the permanent GUID.

- **Shape:** 12 characters in three dash-separated blocks of four — `XXXX-XXXX-XXXX`.
- **Alphabet (32 symbols, Crockford-style):** `23456789ABCDEFGHJKLMNPQRSTUVWXYZ` — uppercase
  letters and digits with `0`/`O` and `1`/`I` excluded so no pair of glyphs is confusable when
  a code is read aloud or copied by hand.
- **Case:** case-insensitive on input, always displayed and transmitted uppercase.
- **Minting:** server-assigned. The relay generates codes with a cryptographic random source and
  guarantees uniqueness across live codes; there is no client-side minting and therefore no
  collision-resolution protocol on the wire.
- **Normalization on input:** strip dashes and all whitespace, uppercase, then validate against
  the alphabet. Any remaining character outside the alphabet — including `0`, `O`, `1`, `I`, and
  `L`-lookalikes that were never mintable — makes the code invalid.
- **Entropy:** 12 symbols over a 32-symbol alphabet is 60 bits. Guessing is not a practical
  attack, and Section 9.5 requires resolution attempts to be rate-limited regardless.

> Adjudicated by the owner, 2026-07-28 (run `plan-spam-3_1-to-3_6`): server-assigned minting;
> Crockford-style alphabet; 4-4-4 dashed blocks; case-insensitive input, uppercase display.

### 6.5 Timestamps

RFC 3339, UTC, `Z` suffix, second precision: `2026-07-28T04:15:30Z`. All timestamps on the wire
are server-assigned except `X-FoodUs-Timestamp`, which is client-assigned and checked for
freshness.

### 6.6 Sequence number

A per-recipient, strictly increasing 64-bit integer the relay assigns to each envelope as it is
queued. It is the cursor for the drain protocol (Section 8.5). Gaps are normal — sequence
numbers are not consecutive after acknowledgement or sweep — and clients must never treat a gap
as an error.

### 6.7 Size limits

The relay enforces these and reports them through `GET /v1/capabilities` so clients can check
before sending.

| Limit | v1 value |
| --- | --- |
| `maxCiphertextBytes` | 262144 (256 KiB, decoded) |
| `maxQueuedEnvelopesPerRecipient` | 1000 |
| `maxDrainBatch` | 100 envelopes per drain call |
| `maxRequestBodyBytes` | 393216 (384 KiB, encoded) |

Exceeding a size limit is `payload_too_large`; exceeding the per-recipient queue depth is
handled as described in Section 8.4.

---

## 7. Envelope Schema

An envelope is the unit of store-and-forward. Its payload is opaque: the relay routes it and
never reads it.

### 7.1 Fields as pushed by the sender

| Field | Type | Required | Meaning |
| --- | --- | --- | --- |
| `envelopeVersion` | integer | yes | Payload contract version. v1 relays support `1`. |
| `recipientGuid` | GUID | yes | Whose queue this envelope joins. |
| `senderGuid` | GUID | yes | Must equal the authenticated `X-FoodUs-Guid`; the relay rejects a mismatch as `unauthorized`. |
| `ciphertext` | base64url string | yes | The sealed payload, encrypted by the sender to the recipient's public key. Opaque to the relay. |

The relay must not require, define, or inspect any structure inside `ciphertext`. Everything the
recipient needs to reconstruct an entry — nutrition, name, entry timestamp, meal name, message
kind — lives inside the sealed payload and is the app's business, not the contract's.

`senderGuid` and `recipientGuid` are necessarily in the clear: the relay needs the recipient to
route, and the recipient needs the sender to select a decryption key and to enforce its own
local block list. This metadata exposure is the deliberate, bounded cost of store-and-forward.

### 7.2 Fields the relay adds on acceptance

| Field | Type | Meaning |
| --- | --- | --- |
| `envelopeId` | GUID | Server-assigned, unique, used for acknowledgement. |
| `sequence` | integer | Per-recipient cursor value (Section 6.6). |
| `receivedAt` | timestamp | When the relay queued it. |
| `expiresAt` | timestamp | `receivedAt` + 30 days (Section 10). |

Server-assigned fields are ignored if a client sends them.

### 7.3 Example

Pushed by the sender:

```json
{
  "envelopeVersion": 1,
  "recipientGuid": "6f1c2d18-8f0e-4d6a-9a1b-3c5e7f9a0b21",
  "senderGuid": "b2d4c6e8-1a3b-4c5d-8e9f-0a1b2c3d4e5f",
  "ciphertext": "T2hIYWlUaGlzSXNTZWFsZWRDaXBoZXJ0ZXh0T25seQ"
}
```

Returned on drain:

```json
{
  "envelopeVersion": 1,
  "envelopeId": "1f0a9c77-2b44-4f8e-8c31-7d6e5a4b3c2d",
  "sequence": 4187,
  "recipientGuid": "6f1c2d18-8f0e-4d6a-9a1b-3c5e7f9a0b21",
  "senderGuid": "b2d4c6e8-1a3b-4c5d-8e9f-0a1b2c3d4e5f",
  "ciphertext": "T2hIYWlUaGlzSXNTZWFsZWRDaXBoZXJ0ZXh0T25seQ",
  "receivedAt": "2026-07-28T04:15:31Z",
  "expiresAt": "2026-08-27T04:15:31Z"
}
```

---

## 8. Endpoints

Seven endpoints make up the v1 surface. All except `GET /v1/capabilities` require the
authentication headers of Section 5.2.

### 8.1 Register or update profile

```text
POST /v1/profile
```

Creates a profile on first call for a GUID and updates it thereafter. This is the single seam
for registering the crypto identity, renaming, and announcing a re-key.

**Request**

| Field | Type | Required | Notes |
| --- | --- | --- | --- |
| `guid` | GUID | yes | Must equal `X-FoodUs-Guid`. |
| `username` | string | yes | Cosmetic (Section 6.2). |
| `publicKey` | base64url | yes | Current public key (Section 6.3). |
| `previousKeySignature` | base64url | only when `publicKey` differs from the registered key | Old key's signature over the re-key statement (Section 5.6). |

```json
{
  "guid": "b2d4c6e8-1a3b-4c5d-8e9f-0a1b2c3d4e5f",
  "username": "Jay",
  "publicKey": "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE...",
  "previousKeySignature": "MEUCIQDf1Xk9...redacted-example..."
}
```

**Response `200 OK`** — the profile as stored, including the friend code the relay minted on
first registration.

```json
{
  "guid": "b2d4c6e8-1a3b-4c5d-8e9f-0a1b2c3d4e5f",
  "username": "Jay",
  "publicKey": "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE...",
  "friendCode": "A7F3-9KQ2-XM41",
  "createdAt": "2026-07-28T04:10:00Z",
  "updatedAt": "2026-07-28T04:15:30Z"
}
```

**Notes**

- First registration follows the trust-on-first-use rule of Section 5.5.
- Changing `publicKey` follows Section 5.6; without a valid `previousKeySignature` the request
  is `unauthorized`.
- The friend code is assigned once at first registration and only ever changes through
  Section 8.3.
- Repeating the call with identical values is a successful no-op.

### 8.2 Resolve friend code

```text
POST /v1/friend-code/resolve
```

Turns a friend code into the three things needed to become friends. Resolution **is** the key
exchange.

**Request**

```json
{ "friendCode": "a7f3-9kq2-xm41" }
```

The relay normalizes the code per Section 6.4 before lookup.

**Response `200 OK`**

```json
{
  "guid": "6f1c2d18-8f0e-4d6a-9a1b-3c5e7f9a0b21",
  "username": "Sam",
  "publicKey": "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE..."
}
```

**Block enforcement — the indistinguishability rule.** The relay returns the *identical*
`404 not_found` response (Section 9.3) in all of these cases:

- the code has never existed;
- the code is syntactically valid but not currently assigned;
- the code was regenerated and this is a dead copy;
- the code resolves to a profile whose owner has blocked the caller.

The blocked caller learns nothing — not that the target exists, not that a block is in place,
not that a fresh code would help. A newly regenerated code is equally invisible to them.

### 8.3 Regenerate friend code

```text
POST /v1/friend-code/regenerate
```

Mints a new server-assigned code for the authenticated GUID and immediately retires the previous
one. No request body is required; send an empty body and hash it accordingly (Section 5.2).

**Response `200 OK`**

```json
{ "friendCode": "TQ92-KD4H-7VBM" }
```

**Notes**

- Every previously distributed copy of the old code dies instantly and thereafter resolves to
  the standard `not_found` (Section 8.2).
- **Existing friendships are unaffected.** Friendships are held device-side against the GUID,
  which never changes; the code is only the introduction handle.
- The relay must not retain retired codes in a way that lets them be resolved again.

### 8.4 Push sealed message

```text
POST /v1/messages
```

Appends one envelope to the recipient's queue.

**Request:** the envelope of Section 7.1. `senderGuid` must equal the authenticated GUID.

**Response `202 Accepted`**

```json
{ "accepted": true }
```

**Delivery is opaque to the sender.** The relay returns the same `202 Accepted` body whether the
envelope was queued or silently discarded, and discards it when:

- `recipientGuid` matches no profile; or
- the recipient has blocked the sender; or
- the recipient's queue is at `maxQueuedEnvelopesPerRecipient`.

This is the push-side expression of the same indistinguishability rule as Section 8.2: a blocked
sender must not be able to detect their block by watching push results, so a push to a
nonexistent recipient must look identical too. The cost is that an envelope addressed to a
mistyped GUID vanishes without a signal — acceptable, because GUIDs are exchanged by resolving a
friend code rather than typed by hand.

Rejections that do **not** depend on the recipient are reported normally: `bad_request` for a
malformed envelope, `unsupported_envelope_version` for an unknown `envelopeVersion` (Section
4.4), `payload_too_large` for an oversized ciphertext, and `unauthorized` for a `senderGuid`
that does not match the signer.

### 8.5 Poll and drain mailbox

```text
GET /v1/messages?afterSequence=<int>&limit=<int>
POST /v1/messages/acknowledge
```

Poll-on-wake drain for the authenticated GUID. A caller can only ever read its own queue: the
queue is selected by the authenticated GUID and there is no parameter to name another.

**`GET /v1/messages`**

| Query parameter | Required | Meaning |
| --- | --- | --- |
| `afterSequence` | no | Return only envelopes with a sequence strictly greater than this. Omit to start from the beginning of the queue. |
| `limit` | no | Maximum envelopes to return, 1 to `maxDrainBatch`. Defaults to `maxDrainBatch`. |

**Response `200 OK`** — envelopes in ascending `sequence` order (oldest first), each in the
drained form of Section 7.3.

```json
{
  "envelopes": [ { "envelopeVersion": 1, "sequence": 4187, "...": "..." } ],
  "hasMore": false,
  "highestSequence": 4187
}
```

**Acknowledgement is explicit and selective.** Reading an envelope does not delete it. The
client deletes what it has successfully processed by acknowledging it:

```text
POST /v1/messages/acknowledge
```

```json
{ "envelopeIds": ["1f0a9c77-2b44-4f8e-8c31-7d6e5a4b3c2d"] }
```

**Response `200 OK`**

```json
{ "acknowledged": 1 }
```

Acknowledging an unknown, already-acknowledged, or swept `envelopeId` is not an error; it is
counted as acknowledged so a retried acknowledgement after a dropped response is safe. A client
may only acknowledge envelopes from its own queue; ids from another queue are ignored, never
reported.

**Unknown-version envelopes: leave queued and skip.**

> Adjudicated by the owner, 2026-07-28 (run `rails-boss-execute`), settling the open contract
> question flagged by the app repository.

When a client drains an envelope whose `envelopeVersion` it does not understand, it:

1. **refuses loudly** — surfaces the refusal to the user rather than dropping it silently;
2. **does not acknowledge it**, so the envelope stays queued and remains readable by a future
   app version that understands it; and
3. **skips past it** by continuing the drain with `afterSequence` set to that envelope's
   sequence, so the unreadable envelope never blocks the envelopes behind it.

This is why the cursor and the acknowledgement are separate mechanisms. Polling therefore never
permanently fails on an envelope the client cannot read, and no message is destroyed merely
because it arrived early. An unreadable envelope that is never acknowledged is removed by the
30-day sweep (Section 10) like any other undelivered envelope.

### 8.6 Record block

```text
POST /v1/blocks
```

Records that the authenticated GUID blocks another GUID. The block is enforced by the relay at
resolution (Section 8.2) and at push (Section 8.4).

**Request**

```json
{ "blockedGuid": "6f1c2d18-8f0e-4d6a-9a1b-3c5e7f9a0b21" }
```

**Response `200 OK`**

```json
{ "blocked": true }
```

**Notes**

- Idempotent. Blocking an already-blocked GUID succeeds unchanged.
- The response is identical whether `blockedGuid` names a real profile or not, so recording a
  block cannot be used to probe for the existence of a GUID.
- The relay never notifies the blocked party, and there is no endpoint that lists who has
  blocked whom. A caller can only ever read or write its own block list.
- v1 has no unblock endpoint. Blocking is deliberately permanent from the relay's side; the app
  presents it as such. Adding unblock later is an additive amendment.

### 8.7 Version and capability query

```text
GET /v1/capabilities
```

Unauthenticated. Answers "what version are you, and what do you support" so clients adapt at
runtime instead of assuming. It doubles as the liveness signal.

**Response `200 OK`**

```json
{
  "contractMajor": 1,
  "contractMinor": 0,
  "envelopeVersions": [1],
  "capabilities": ["profile", "friend-codes", "blocks", "mailbox"],
  "limits": {
    "maxCiphertextBytes": 262144,
    "maxQueuedEnvelopesPerRecipient": 1000,
    "maxDrainBatch": 100,
    "maxRequestBodyBytes": 393216
  },
  "retentionDays": 30,
  "serverTime": "2026-07-28T04:15:30Z"
}
```

**Notes**

- `capabilities` is an open-ended list of names. Clients match on names they know and ignore the
  rest; a name absent from the list means the feature is unavailable and must be hidden or
  greyed.
- `envelopeVersions` lists every version the relay will accept at push. Clients stamp with the
  highest version they and the relay both support.
- `serverTime` exists so a client with a skewed clock can correct its offset before signing
  (Section 5.4).
- The response exposes no user data and no operator-private value.

---

## 9. Error Semantics

### 9.1 Uniform error shape

Every error response — with the single exception of the fixed body in Section 9.3 — is this
object and nothing more:

```json
{ "error": { "code": "bad_request", "message": "envelopeVersion is required" } }
```

`code` is a stable machine-readable string; clients branch on it. `message` is human-readable
debugging aid; clients must never parse it, and it must never contain user data, key material,
or internal detail.

| `code` | HTTP status | When |
| --- | --- | --- |
| `bad_request` | 400 | Malformed JSON, missing or invalid field, bad GUID or key encoding |
| `unsupported_envelope_version` | 400 | `envelopeVersion` not in the relay's `envelopeVersions` |
| `unauthorized` | 401 | Any authentication or replay-protection failure (Section 9.2) |
| `not_found` | 404 | Friend code unresolvable **or** caller blocked (Section 9.3) |
| `payload_too_large` | 413 | A Section 6.7 size limit exceeded |
| `rate_limited` | 429 | Section 9.5 |
| `internal_error` | 500 | Unexpected server fault; never carries diagnostic detail |

Clients apply the same two-way tolerance to error objects as to any other object: an unknown
`code` is handled as a generic failure, never a crash.

### 9.2 Authentication failures collapse to one response

Every one of these returns an identical `401` with `code: "unauthorized"` and a fixed generic
message:

- missing, duplicated, or malformed authentication headers;
- signature verification failure;
- `X-FoodUs-Body-Sha256` not matching the body actually received;
- timestamp outside the freshness window;
- replayed nonce;
- `senderGuid` or `guid` in the body not matching the authenticated GUID;
- a `publicKey` change without a valid `previousKeySignature`.

The relay never reveals which check failed. Telling a caller "signature valid but timestamp
stale" hands an attacker a free oracle.

### 9.3 Blocked and nonexistent are byte-identical

This is a constitutional requirement, not a convention. For an unresolvable friend code and for
a code whose owner has blocked the caller, the relay returns **exactly** the same bytes:

```http
HTTP/1.1 404 Not Found
Content-Type: application/json; charset=utf-8

{"error":{"code":"not_found","message":"user not found"}}
```

The status code, the header set, the `code`, and the `message` are identical in both cases. No
extra header, no varying `Content-Length`, no differing whitespace, no diagnostic hint. Any
divergence in the response — however small — reintroduces the oracle the rule exists to close.

**Timing side channel.** A byte-identical body is not enough on its own. A block check that runs
only after a successful code lookup takes measurably longer than a lookup that misses, which
leaks the existence of the profile to an attacker who measures response latency. Implementations
must not let the two paths diverge observably: perform the same work in both cases — resolve,
then evaluate the block, then discard the result on either failure — rather than returning early
on the first miss. Where the work genuinely cannot be equalized, normalize the response time.
This obligation belongs to the relay implementation (Milestone 3 Story 4) and its tests.

### 9.4 Unknown envelope version

At push, an `envelopeVersion` the relay does not support is refused loudly with `400` and
`code: "unsupported_envelope_version"`; the envelope is never queued. The client surfaces the
refusal rather than retrying blindly. At drain, an unknown version is handled by the client per
the leave-queued rule of Section 8.5.

### 9.5 Rate limiting

The relay rate-limits per authenticated GUID and per source address, and returns `429` with
`code: "rate_limited"` and a `Retry-After` header when a limit trips. Friend-code resolution in
particular must be rate-limited so the 60-bit code space cannot be swept. Concrete thresholds
are an operator concern, not a contract value; clients must handle `429` and back off rather
than assuming a specific limit.

---

## 10. Retention

- **Undelivered envelopes are deleted 30 days after `receivedAt`.** The relay sweeps them on a
  schedule; `expiresAt` on every envelope states the deadline explicitly and
  `retentionDays` in `GET /v1/capabilities` publishes the policy.
- Acknowledged envelopes are deleted immediately on acknowledgement (Section 8.5).
- The sweep applies to every queued envelope regardless of version, including one a client left
  queued because it could not read it.
- The relay stores nothing else that ages: profiles, friend codes, and block relationships
  persist until changed by their owner. Nonce cache entries persist only for the replay window
  (Section 5.4).
- There is no archive, no backup, and no recovery of a swept envelope. The precious data lives
  on the phones.

---

## 11. Change Log

Every additive amendment inside major version 1 appends a row here and increments the contract
minor version reported by `GET /v1/capabilities`. Breaking changes do not appear here; they
stand up a new major route family with its own document.

| Contract version | Date | Change |
| --- | --- | --- |
| 1.0 | 2026-07-28 | Initial contract. Transport, detached-signature authentication with replay window and old-key-signed re-key, data types, envelope schema, the seven v1 endpoints, error semantics with blocked/nonexistent indistinguishability, and 30-day retention. |
