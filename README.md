
# InterFAX .NET Library

[![Build Status](https://travis-ci.org/interfax/interfax-dotnet.svg?branch=master)](https://travis-ci.org/interfax/interfax-dotnet)

[Installation](#installation) | [Getting Started](#getting-started) | [Contributing](#contributing) | [License](#license)

Send and receive faxes in [CLI Languages](https://en.wikipedia.org/wiki/List_of_CLI_languages) with the [InterFAX REST API](https://www.interfax.net/en/dev/rest/reference).

(examples are in C#)

## Installation

This library targets .NET 4.5.2 and can be installed via Nuget :

```
Install-Package InterFAX.Api -Version 1.0.5
```

## Getting started


# Usage

[Client](#client) | [Account](#account) | [Outbound](#outbound) | [Inbound](#inbound) | [Documents](#documents)

## Client

The client follows the [12-factor](http://12factor.net/config) apps principle and can be either set directly, via configuration files or environment variables.

```csharp
using InterFAX.Api;

// Initialize using parameters
var interfax = new FaxClient(username: "...", password: "...");

// Initialize using App.config or Environment variables (both have the same key)
// NB : App.config will take precedence over environment variables.
// <appSettings>
//   <add key="INTERFAX_USERNAME" value="username"/>
//   <add key="INTERFAX_PASSWORD" value="password"/>
// </appSettings>

var interfax = new FaxClient();
```

All connections are established over HTTPS.

## Account

### Balance

`async Task<decimal> GetBalance();`

Determine the remaining faxing credits in your account.

```csharp
var interfax = new FaxClient();

var balance = await interfax.Account.GetBalance();
Console.WriteLine($"Account balance is {balance}"); //=> Account balance is 9.86
```

**More:** [documentation](https://www.interfax.net/en/dev/rest/reference/3001)

## Outbound

[Send](#send-fax) | [Get list](#get-outbound-fax-list) | [Get completed list](#get-completed-fax-list) | [Get record](#get-outbound-fax-record) | [Get image stream](#get-outbound-fax-image-stream) | [Cancel fax](#cancel-a-fax) | [Search](#search-fax-list)

### Send fax

`async Task<int> SendFax(IFaxDocument faxDocument, SendOptions options)`
`async Task<int> SendFax(List<IFaxDocument> faxDocuments, SendOptions options)`

Submit a fax to a single destination number. Returns the messageId of the fax.

For small documents, there are a few ways to send a fax. You can directly provide a file path, a file stream or a url, which can be a web page anywhere or a link to a previously uploaded document resource (see [Documents](#documents)).

```csharp
var options = new SendOptions { FaxNumber = "+11111111112"};

// with a path
var fileDocument = interfax.Documents.BuildFaxDocument(@".\folder\fax.txt");
var messageId = await interfax.SendFax(faxDocument, options);

// with a stream
// NB : the caller is responsible for opening and closing the stream.
using (var fileStream = File.OpenRead(@".\folder\fax.txt"))
{
  var fileDocument = interfax.Documents.BuildFaxDocument("fax.txt", fileStream);
  var messageId = await interfax.SendFax(faxDocument, options);
}

// with a URL
var urlDocument = interfax.Documents.BuildFaxDocument(new Uri("https://s3.aws.com/example/fax.html"));
var messageId = await interfax.SendFax(urlDocument, options);

// or a combination
var documents = new List<IFaxDocument> { fileDocument, urlDocument };
var messageId = await interfax.SendFax(documents, options)
```

InterFAX supports over 20 file types including HTML, PDF, TXT, Word, and many more. For a full list see the [Supported File Types](https://www.interfax.net/en/help/supported_file_types) documentation.
The supported types are mapped to media types in the file SupportedMediaTypes.json - this can be modified by hand.

The returned object is the Id of the fax. Use this Id to load more information, get the image, or cancel the sending of the fax.

```csharp
var result = await interfax.CancelFax(messageId);

// result in this case is just "OK".
```
**SendOptions:** [`FaxNumber`, `Contact`, `PostponeTime`, `RetriesToPerform`, `Csid`, `PageHeader`, `Reference`, `PageSize`, `FitToPage`, `PageOrientation`, `Resolution`, `Rendering`](https://www.interfax.net/en/dev/rest/reference/2918)

---

### Get outbound fax list

`async Task<IEnumerable<OutboundFaxSummary>> GetList(Outbound.ListOptions options = null);`

Get a list of recent outbound faxes (which does not include batch faxes).

```csharp
var faxes = await interfax.Outbound.GetList();
```

**Outbound.ListOptions:** [`Limit`, `LastId`, `SortOrder`, `UserId`](https://www.interfax.net/en/dev/rest/reference/2920)

---

### Get completed fax list

`async Task<IEnumerable<OutboundFaxSummary>> GetCompleted(params int[] ids)`

Get details for a subset of completed faxes from a submitted list. (Submitted id's which have not completed are ignored).

```csharp
var completed = await interfax.Outbound.GetCompleted(1, 2, 3);
```

**More:** [documentation](https://www.interfax.net/en/dev/rest/reference/2972)

----

### Get outbound fax record

`async Task<OutboundFaxSummary> GetFaxRecord(int id);`

Retrieves information regarding a previously-submitted fax, including its current status.

```csharp
var fax = interfax.Outbound.GetFaxRecord(123456)
```

**More:** [documentation](https://www.interfax.net/en/dev/rest/reference/2921)

---

### Get outbound fax image stream

`async Task<Stream> GetFaxImageStream(int id);`

Retrieve the fax image stream (TIFF file) of a submitted fax.

```csharp
using (var imageStream = await _interfax.Outbound.GetFaxImageStream(662208217))
{
    using (var fileStream = File.Create(@".\image.tiff"))
    {
        InterFAX.Utils.CopyStream(imageStream, fileStream);
    }
}
```

**More:** [documentation](https://www.interfax.net/en/dev/rest/reference/2941)

----

### Cancel a fax

`async Task<string> CancelFax(int id)`

Cancel a fax in progress.

```csharp
var result = interfax.Outbound.CancelFax(123456)

// => OK
```

**More:** [documentation](https://www.interfax.net/en/dev/rest/reference/2939)

----

### Search fax list

`async Task<IEnumerable<OutboundFax>> SearchFaxes(SearchOptions searchOptions)`

Search for outbound faxes.

```csharp
var faxes = await interfax.Outbound.Search(new SearchOptions {
  faxNumber = '+1230002305555'
});
```

**Options:** [`Ids`, `Reference`, `DateFrom`, `DateTo`, `Status`, `UserId`, `FaxNumber`, `Limit`, `Offset`](https://www.interfax.net/en/dev/rest/reference/2959)

## Inbound

[Get list](#get-inbound-fax-list) | [Get record](#get-inbound-fax-record) | [Get image stream](#get-inbound-fax-image-stream) | [Get emails](#get-forwarding-emails) | [Mark as read](#mark-as-readunread) | [Resend to email](#resend-inbound-fax)

### Get inbound fax list

`async Task<IEnumerable<InboundFax>> GetList(ListOptions listOptions = null)`

Retrieves a user's list of inbound faxes. (Sort order is always in descending ID).

```csharp
var faxes = await interfax.Inbound.GetList(new ListOptions { UnreadOnly = true });
```

**Options:** [`UnreadOnly`, `Limit`, `LastId`, `AllUsers`](https://www.interfax.net/en/dev/rest/reference/2935)

---

### Get inbound fax record

`async Task<InboundFax> GetFaxRecord(int id)`

Retrieves a single fax's metadata (receive time, sender number, etc.).

```csharp
var fax = await interfax.Inbound.GetFaxRecord(123456);
```

**More:** [documentation](https://www.interfax.net/en/dev/rest/reference/2938)

---

### Get inbound fax image stream

`async Task<Stream> GetFaxImageStream(int id)`

Retrieves a single fax's image.

```csharp
using (var imageStream = await _interfax.Inbound.GetFaxImageStream(291704306))
{
    using (var fileStream = File.Create(@".\image.tiff"))
    {
        Utils.CopyStream(imageStream, fileStream);
    }
}
```

**More:** [documentation](https://www.interfax.net/en/dev/rest/reference/2937)

---

### Get forwarding emails

`async Task<IEnumerable<ForwardingEmail>> GetForwardingEmails(int id)`

Retrieve the list of email addresses to which a fax was forwarded.

```csharp
var emails = await interfax.Inbound.GetForwardingEmails(12345);
foreach(var email in emails)
	Console.WriteLine($"{email.EmailAddress}, {email.MessageStatus}, {email.CompletionTime.ToString("s")}")
```

**More:** [documentation](https://www.interfax.net/en/dev/rest/reference/2930)

---

### Mark as read/unread

`async Task<string> MarkRead(int id)`
`async Task<string> MarkUnread(int id)`

Mark a transaction as read/unread.

```csharp
// mark as read
var result = interfax.Inbound.MarkRead(123456);

// => OK

// mark as unread
var result = interfax.Inbound.MarkUnread(123456);

// => OK
```

**More:** [documentation](https://www.interfax.net/en/dev/rest/reference/2936)

### Resend inbound fax

`async Task<string> Resend(int id, string emailAddress = null)`

Resend an inbound fax, optionally to a specific email address.

```csharp
// resend to the email(s) to which the fax was previously forwarded
var result = await interfax.Inbound.Resend(123456);

// => OK

// resend to a specific address
var result = await interfax.Inbound.Resend(123456) "test@example.com");

// => OK
```

**More:** [documentation](https://www.interfax.net/en/dev/rest/reference/2929)

---

## Documents

[Create document upload session](#create-document-upload-session) | [Upload chunk](#upload-chunk) | [Get upload session list](#get-upload-session-list) | [Status](#get-upload-session-status) | [Cancel](#cancel-document-upload-session)

Document upload sessions allow for uploading of large files up to 20MB in chunks of arbitrary size.

You can do this with either a file path :

`UploadSession UploadDocument(string filePath)`

```csharp
var fileInfo = new FileInfo("test.pdf"));
var session = _interfax.Outbound.Documents.UploadDocument(fileInfo.FullName);
```

Or with a file stream :

`UploadSession UploadDocument(string fileName, FileStream fileStream)`

```csharp
using (var fileStream = File.OpenRead("test.pdf"))
{
  var session = _interfax.Outbound.Documents.UploadDocument(filePath, fileStream);
}
```

The Uri property of the returned session object can be used when sending a fax.

If you want to control the chunking yourself, the following example shows how you could upload a file in 500 byte chunks:

```csharp
var fileInfo = new FileInfo("large.pdf");

var sessionId = CreateUploadSession(new UploadSessionOptions
{
    Name = fileInfo.Name,
    Size = (int) fileInfo.Length
}).Result;

using (var fileStream = File.OpenRead(filePath))
{
    var buffer = new byte[500];
    int len;
    while ((len = fileStream.Read(buffer, 0, buffer.Length)) > 0)
    {
        var data = new byte[len];
        Array.Copy(buffer, data, len);
        var response = UploadDocumentChunk(sessionId, fileStream.Position - len, data).Result;
        if (response.StatusCode == HttpStatusCode.Accepted) continue;
        if (response.StatusCode == HttpStatusCode.OK) break;
    }
}
```

### Create Document Upload Session

`async Task<string> CreateUploadSession(UploadSessionOptions options)`

Create a document upload session, allowing you to upload large files in chunks.

```csharp
var sessionId = _interfax.Outbound.Documents.CreateUploadSession(options);
```

**Options:** [`Disposition`, `Sharing`](https://www.interfax.net/en/dev/rest/reference/2967)

---

### Upload chunk

`async Task<HttpResponseMessage> UploadDocumentChunk(string sessionId, long offset, byte[] data)`

Upload a chunk to an existing document upload session. The offset refers to the offset in the unchunked file not the data parameter.

```csharp
var response = UploadDocumentChunk(sessionId, offset, data).Result;

// HttpStatusCode.OK (done) or HttpStatusCode.Accepted (unfinished)
```

**More:** [documentation](https://www.interfax.net/en/dev/rest/reference/2966)

---

### Get Upload Session List

`async Task<IEnumerable<UploadSession>> GetUploadSessions(ListOptions listOptions = null)`

Get a list of previous document uploads which are currently available.

```csharp
var list = _interfax.Outbound.Documents.GetUploadSessions(new Documents.ListOptions
                {
                    Offset = 10,
                    Limit = 5
                }).Result;
```

**Options:** [`Limit`, `Offset`](https://www.interfax.net/en/dev/rest/reference/2968)

---

### Get upload session status

`async Task<UploadSession> GetUploadSession(string sessionId)`

Get the current status of a specific document upload session.

```csharp
var session = interfax.Documents.GetUploadSession(123456);
```

**More:** [documentation](https://www.interfax.net/en/dev/rest/reference/2965)

---

### Cancel document upload session

`interfax.documents.cancel(document_id, callback);`

Cancel a document upload and tear down the upload session, or delete a previous upload.

```csharp
var result = await interfax.Outbound.Documents.CancelUploadSession(sessionId);

// => OK
```

**More:** [documentation](https://www.interfax.net/en/dev/rest/reference/2964)

---

## Contributing

 1. **Fork** the repo on GitHub
 2. **Clone** the project to your own machine
 3. **Commit** changes to your own branch
 4. **Push** your work back up to your fork
 5. Submit a **Pull request** so that we can review your changes

## License

This library is released under the [MIT License](LICENSE).
