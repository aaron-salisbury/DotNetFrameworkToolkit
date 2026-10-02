# Credential persistence

The toolkit supplies credential creation/verification, not an account store or an authentication policy. Applications own password rules, storage, reset flows, and configuration.

| Current stored field | Meaning |
| --- | --- |
| `LoginSalt` | Random salt bytes. Persist the bytes exactly; a text store can use Base64. Accepted stored length is 8–1024 bytes. |
| `LoginHash` | Exactly 32 bytes derived with PBKDF2-HMAC-SHA1 from `Encoding.UTF8.GetBytes(password)`, the stored salt, and the stored iteration count. |
| `LoginWorkFactor` | Positive iteration count. Verification uses this stored count, not the current creation setting, and rejects counts above the configured verification ceiling. |

Persist all three fields together. Verification does not change credentials, increase their work factor, or automatically write an upgraded record. Changing the salt length or creation work factor affects new credentials; existing records still verify if within the verification bounds. Lowering the verification ceiling can reject previously accepted records.

Configuration is snapshotted when the authenticator is constructed. It does not follow later mutations of the configuration object. Stored arrays are copied for verification, but callers must not concurrently mutate the credential DTO or its arrays. The DTO is mutable and has no thread-safety guarantee.

Passwords are encoded as supplied: there is no Unicode normalization, trimming, or case conversion. Empty passwords are accepted by the helper; application policy must decide whether to allow them. Null passwords/credentials throw; malformed stored lengths/counts return false. Hash comparison examines every hash byte.

## Format changes

The current DTO has **no algorithm or format identifier**. The earlier ASCII/repeated-salted-hash format was deliberately removed during the 0.x changes. It is not tried as a fallback. Legacy records need an application-controlled reset or explicit migration using separately identified legacy records; their hashes cannot be converted to the current format without the password.

Future changes to password encoding, derivation algorithm, hash length, or field meanings must introduce an explicit format discriminator and a documented migration/reset path before shipping. Do not reinterpret unversioned records, guess an algorithm from their bytes, or silently attempt multiple formats. An application planning multiple formats should store its own format identifier alongside these fields now.

Phase 4 retains the existing format. Fixed ASCII and Unicode test vectors protect stored-record compatibility. Changing only a new-record work factor within the same algorithm does not change the format because each record already stores its count.

The default work factor is a compatibility default, not a deployment recommendation. Applications must calibrate their settings for their environment. Never log passwords, salts, or hashes.
