# SOAP integration notes

This project uses REST and JSON for its real application API. It does not claim to provide a production SOAP integration. These notes show how the same ASP.NET Core application could consume a legacy SOAP service without moving SOAP concerns into assignment or submission logic.

## The basic idea

SOAP is a protocol for exchanging structured XML messages. A typical request is sent over HTTP and contains an `Envelope`, an optional `Header`, and a `Body`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Header />
  <soap:Body>
    <GetStudentStatus xmlns="https://legacy.example.com/school">
      <studentId>42</studentId>
    </GetStudentStatus>
  </soap:Body>
</soap:Envelope>
```

The HTTP request commonly uses `text/xml` or `application/soap+xml`. SOAP 1.1 services may also require a `SOAPAction` header that identifies the operation. A WSDL document describes operations, message shapes, types, endpoints, and bindings. Visual Studio's Connected Services or `dotnet-svcutil` can generate a typed C# proxy from a WSDL when that is more maintainable than building envelopes manually.

## REST and SOAP in this project’s context

| REST API used here | Typical SOAP service |
|---|---|
| Routes such as `GET /api/assignments` | Operations such as `GetAssignments` |
| JSON by default, plus one explicit XML export | XML envelopes are required |
| HTTP status codes plus Problem Details | SOAP Fault elements carry protocol errors |
| OpenAPI describes the HTTP API | WSDL describes the SOAP contract |
| Lightweight for the React client | Common in older banking, travel, government, and enterprise platforms |

## How ASP.NET Core could consume one

Keep an interface such as `ILegacyStudentRegistryClient` beside other integrations. Its implementation can use a generated proxy or an `HttpClient` created by `IHttpClientFactory`. It would construct or serialize the approved envelope, set the correct content type and `SOAPAction`, use a short timeout, send a cancellation token, check the HTTP status, parse the expected XML with external entity resolution disabled, and translate SOAP Faults into a small application result.

The controller or service should depend on the interface, not XML strings. Tests should use a fake `HttpMessageHandler`; a real external SOAP service should never be required for local tests. Credentials belong in a secret manager. Do not log credentials, the entire envelope, or personal data.

## Faults and validation

SOAP faults are structured XML inside the response envelope. A client must handle both HTTP failures and `<Fault>` content. If manual parsing is needed, use `XmlReaderSettings` with `DtdProcessing = Prohibit` and `XmlResolver = null` to prevent external-entity attacks. Validate expected namespaces and cap response sizes at the HTTP-client or proxy boundary.

## Interview wording

“The production feature in this repository is a REST API. I added XML export and studied how an isolated `HttpClient`-based SOAP client would work, including WSDL, SOAPAction, envelopes, faults, timeouts, secure XML parsing, and mocked tests. I would not claim this project has a live production SOAP integration.”
