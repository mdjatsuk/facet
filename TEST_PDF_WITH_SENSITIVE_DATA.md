# Test Instructions

## The issue might be:

1. **PDF has no searchable text** - If your PDF is a scanned image, PdfPig can't extract text (needs OCR)
2. **Frontend not showing the data** - The data is detected but not displayed
3. **API not being called** - Check browser network tab

## Quick Test:

### Step 1: Test the scanner directly

Create a simple text file named `test.txt` with this content:
```
Contact: john.doe@example.com
Phone: +372 5555 1234
IBAN: EE382200221020145685
ID: 38501234567
```

### Step 2: Check if your PDF has searchable text

Try to open your PDF and select/copy text from it. If you can't select text, the PDF is a scanned image and won't work without OCR.

### Step 3: Check the browser console

1. Open browser DevTools (F12)
2. Go to Network tab
3. Upload a PDF
4. Look for the `/api/documents/upload` request
5. Click on it and check the **Response** tab
6. You should see something like:
```json
{
  "success": true,
  "message": "Fail on edukalt üles laetud",
  "document": { ... },
  "detected": [
    { "type": "email", "value": "test@example.com", ... }
  ]
}
```

### Step 4: Check backend logs

The backend should show logs like:
```
info: Starting PDF scan, stream length: 12345
info: PDF opened successfully, pages: 1
info: Page 1 has 234 characters
info: Total extracted text length: 234
info: Scanning text of length 234
info: Found 3 sensitive items
```

## If detected array is empty or null:

The PDF either:
- Has no searchable text (is a scanned image)
- Uses special fonts/encoding that PdfPig can't read
- Actually contains no sensitive data patterns

## Solution for scanned PDFs:

You need OCR (Optical Character Recognition). I can add Tesseract support if needed.

## Quick fix to test:

Stop the backend (Ctrl+C in the terminal) and I'll restart it with the new logging enabled.
