# publish-ats

A little tool that I wrote for myself to parse my CV and convert it to ATS-friendly formats. It uses local NLP model from Microsoft ML library to enhance the CV content and make it more compatible with Applicant Tracking Systems (ATS).

## Features

- **Format Conversion**:
    - From DOC/DOCX 
    - To MD/DOC/DOCX/PDF
- **ATS Optimization**:
    - Process content to improve compatibility with Applicant Tracking Systems
    - Apply local NLP model to enhance keyword recognition
    - Structure content for better parsing by automated systems

## Usage examples

- Convert CV to markdown and add ATS tags:
    ```bash
    publish-ats -i cv.docx -f md -a
    ```
    - Convert a Markdown CV to PDF and DOCX formats with ATS optimization:
   ```bash
    publish-ats -i cv.md -f pdf,docx -a
    ```
    - Convert a Word document to PDF and Markdown with a custom output filename:
  ```bash
    publish-ats -i resume.docx -o optimized_resume -f pdf,md
    ```

## CLI Options

| Option                   | Description                                       |
|--------------------------|---------------------------------------------------|
| `-i, --input <file>`     | Input markdown or Word file path                  |
| `-o, --output <file>`    | Output file path (optional)                       |
| `-f, --format <formats>` | Output formats (comma-separated: pdf,docx,doc,md) |
| `-a, --ats`              | Optimize for Applicant Tracking Systems           |
| `-h, --help`             | Display help message                              |