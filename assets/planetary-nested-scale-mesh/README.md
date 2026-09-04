# New Earth Renderer topology assets

This directory contains the accepted 18-level immutable NCSM1 scale-mesh
library used by NovaCore's New Earth Renderer. The manifest records ordered
level identity, byte length, and hashes; runtime loading validates those values
before publication.

The binary topology files are stored with Git LFS. They are presentation
resources only: canonical body-fixed `H(bodyDirection)` remains the physical
terrain authority. The independent generator and integrity tests live in
`NovaCore.Graphics` and `NovaCore.Graphics.Tests`.
