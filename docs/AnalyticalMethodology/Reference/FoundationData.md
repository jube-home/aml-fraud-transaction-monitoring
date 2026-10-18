---
layout: default
title: Numeric Data Representation
nav_order: 1
parent: Reference
grand_parent: Analytical Methodology
---

# Numeric data representation

Every analytical method in the product reads and writes one shape of numeric data: a packed `double[]`, described by a
row count and a column count, rather than a data frame, a `DataTable` or a CSV.

- Packing version 1 is IEEE 754 binary64, little endian, row major. For a shape of `Rows` by `Columns`, the packed byte
  length must equal `Rows * Columns * 8`; a caller cannot hand over a six value array for a 3x4 (twelve value) shape.
- The packed array is the one interchange format between a data source, a training run and a trained model's recall:
  nothing downstream re-reads a data frame, a file or a live database connection once a dataset has been packed.
- An expired, foreign or malformed pointer to a packed array reads as not found rather than throwing a parsing error.

This is the structural counterpart to a data frame in R: it has a narrower job (one numeric matrix, no column names, no
mixed types) and no row-recycling behaviour — a shape mismatch is always rejected, never silently repeated to fit.
