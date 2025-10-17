# Sensitive Data Detection - User Guide

## 📍 Where to See Detected Sensitive Data

The detected sensitive data appears in the **"Sensitive Data" box** on the **left side** of your application interface.

### Step-by-Step:

1. **Start both servers:**
   - Backend: `cd Backend/FacetApi && dotnet run` (runs on http://localhost:5000)
   - Frontend: `cd Frontend && npm run dev` (runs on http://localhost:3000)

2. **Open the application:**
   - Navigate to http://localhost:3000 in your browser

3. **Upload a PDF document:**
   - Click the "Upload file" area (or drag & drop a PDF)
   - Upload a PDF that contains sensitive information like:
     - Email addresses (e.g., `john.doe@example.com`)
     - Phone numbers (e.g., `+372 5123 4567`, `555-1234`)
     - IBAN codes (e.g., `EE382200221020145685`)
     - ID codes/numbers (e.g., `38501234567`)

4. **View detected items:**
   - After upload completes, look at the **left sidebar**
   - Below the "Upload file" box, you'll see **"Sensitive Data"** box
   - All detected sensitive items will be listed there with format: `type: value`
   - Example:
     ```
     Sensitive Data    2
     ─────────────────
     • email: john@example.com
     • phone: +372 5123 4567
     ```

## 🎯 What Gets Detected

The system detects these patterns **locally using regex** (no external API calls):

| Type | Pattern | Example |
|------|---------|---------|
| **Email** | Standard email format | `user@domain.com` |
| **Phone** | International/local formats with optional separators | `+372 5123 4567`, `555-1234` |
| **IBAN** | Country code + digits (2-32 chars) | `EE382200221020145685` |
| **ID Code** | Generic 6-11 digit codes | `38501234567` |

## 🔧 How It Works

### Backend (C# / .NET 9)
1. **PDF Text Extraction**: Uses `UglyToad.PdfPig` library to extract text from uploaded PDFs
2. **Pattern Matching**: `SensitiveDataScanner` service runs compiled regex patterns
3. **API Response**: Upload endpoint returns detected items in `UploadResult.Detected` array

**Key Files:**
- `Backend/FacetApi/Services/SensitiveDataScanner.cs` - Detection logic
- `Backend/FacetApi/Services/DocumentService.cs` - Calls scanner after upload
- `Backend/FacetApi/Models/Dtos.cs` - `SensitiveItem` and `UploadResult` models

### Frontend (Nuxt 3 / TypeScript)
1. **Upload Handler**: `UploadDrop.vue` receives detected items from API
2. **State Management**: `index.vue` stores items in `sensitiveItems` ref
3. **Display**: `SensitiveDataBox.vue` shows the list

**Key Files:**
- `Frontend/app/components/UploadDrop.vue` - Emits uploaded doc + detected items
- `Frontend/app/pages/index.vue` - Handles upload event and populates sensitive items
- `Frontend/app/components/SensitiveDataBox.vue` - Displays the list
- `Frontend/app/types/index.ts` - TypeScript types

## 🆚 OpenAI vs Local Detection

### ✅ Local Regex Detection (Current Implementation)
**Pros:**
- ✅ Fast (milliseconds)
- ✅ No external API costs
- ✅ Complete privacy (data never leaves your server)
- ✅ Deterministic and consistent results
- ✅ Works offline
- ✅ Great for known patterns (email, phone, IBAN)

**Cons:**
- ❌ Only detects exact patterns
- ❌ May have false positives (e.g., any 11-digit number matches ID pattern)
- ❌ Cannot detect context-dependent sensitive data
- ❌ No support for obfuscated or misspelled data

### 🤖 OpenAI Detection (Alternative)
**Pros:**
- ✅ Contextual understanding (can identify sensitive data by meaning)
- ✅ Handles variations and obfuscation
- ✅ Can classify ambiguous cases
- ✅ Adapts to new patterns without code changes

**Cons:**
- ❌ Costs money per API call
- ❌ Slower (network latency + processing time)
- ❌ Sends data to external service (privacy/compliance concerns)
- ❌ Non-deterministic (may give different results for same input)
- ❌ Requires internet connection
- ❌ Requires API key management

### 💡 Recommendation
**Use local regex detection** for:
- Known, structured patterns (emails, IBANs, phone numbers)
- High-volume processing
- Privacy-sensitive applications
- Fast response requirements

**Consider OpenAI** if you need:
- Detection of unstructured sensitive information (e.g., "my social security number is...")
- Context-aware classification
- Handling of creative obfuscation
- Multi-language support beyond pattern matching

## 🚀 Future Enhancements

### 1. OCR for Image Files (JPG/PNG)
Currently, only PDFs are scanned. To scan uploaded images:
- Install Tesseract OCR library
- Add `TesseractOCR` NuGet package
- Update `DocumentService.UploadAsync` to call OCR for image files
- Pass extracted text to `_scanner.ScanText()`

### 2. Database Persistence
Store detected items in database:
- Add `SensitiveItems` navigation property to `Document` model
- Create `SensitiveItem` entity with foreign key to `Document`
- Save detected items during upload
- Return them when fetching document details

### 3. Improved Patterns
Customize regex patterns for your region:
- Estonian ID: `^[1-6][0-9]{2}(0[1-9]|1[0-2])(0[1-9]|[12][0-9]|3[01])[0-9]{4}$`
- Estonian phone: `^\+?372\s?[0-9]{7,8}$`
- Specific IBAN countries: `^(EE|LV|LT)[0-9]{2}[A-Z0-9]{1,30}$`

### 4. Background Processing
For large documents:
- Move scanning to background job queue
- Return upload immediately
- Show "scanning..." status in UI
- Update UI when scan completes (via WebSocket or polling)

### 5. Redaction Feature
Add ability to redact/mask detected items:
- Highlight sensitive data in preview
- Click to redact (replace with `***` or black box)
- Generate new PDF with redacted content

## 📝 Testing the Detection

Create a test PDF with this content:
```
Contact Information:
Email: test.user@example.com
Phone: +372 5123 4567
IBAN: EE382200221020145685
ID: 38501234567

Support: support@company.ee
Alternative: +1-555-123-4567
```

Upload it and you should see ~6 detected items in the Sensitive Data box.

## 🐛 Troubleshooting

**No items detected:**
- ✓ Make sure you're uploading a PDF (not image - OCR not implemented yet)
- ✓ Check that PDF contains searchable text (not scanned image)
- ✓ Verify patterns match your data format
- ✓ Check browser console for errors

**False positives:**
- Tighten regex patterns in `SensitiveDataScanner.cs`
- Add additional filtering logic (e.g., check against known safe patterns)
- Implement validation (e.g., IBAN checksum verification)

**Performance issues:**
- Consider async/background scanning for large files
- Add caching for previously scanned documents
- Limit scanning to first N pages for very large PDFs
