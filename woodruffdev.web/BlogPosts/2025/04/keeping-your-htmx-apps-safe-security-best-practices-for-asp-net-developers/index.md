---
title: "Keeping Your htmx Apps Safe: Security Best Practices for ASP.NET Developers"
date: 2025-04-07
categories: 
  - "htmx"
tags: 
  - "asp-net"
  - "asp-net-core"
  - "htmx"
  - "web-development"
  - "webdev"
coverImage: "Keeping-Your-htmx-Apps-Safe-Security-Best-Practices-for-ASP.NET-Developers.png"
---

Alright, developer friends, it’s time to talk about something we all know is essential but often neglect until it’s too late: security. htmx makes building interactive apps with ASP.NET Razor Pages a breeze, but if you’re not careful, you could be opening your app to all sorts of nasty vulnerabilities. Let’s make sure that doesn’t happen.

## Avoiding Common Security Pitfalls

Just because you’re not writing JavaScript doesn’t mean your app is magically secure. Here are some common pitfalls to watch out for:

### 1\. Trusting Client-Side Data

htmx makes it easy to send data to your server using attributes like `hx-post` or `hx-get`. But remember, just because it’s easy to send data doesn’t mean it’s safe.

#### What to Avoid

- Assuming data from the client is safe.

- Using query strings or form data directly without validation.

#### What to Do Instead

- Always validate incoming data on the server.

- Use model binding and server-side validation as your first line of defense.

## Handling Authentication and CSRF Protection

Since htmx uses standard HTTP requests, the same security practices you apply to regular Razor Pages apply here. But there are some things you need to be extra careful about.

### CSRF Protection

ASP.NET Core uses Anti-Forgery Tokens to prevent Cross-Site Request Forgery (CSRF) attacks. And guess what? htmx works well with this mechanism.

#### Adding Anti-Forgery Tokens

Make sure your forms include the `@AntiForgeryToken()` helper:

```html
<form hx-post="/Submit" hx-target="#result">
    @Html.AntiForgeryToken()
    <input type="text" name="comment" placeholder="Add a comment" required>
    <button type="submit">Post Comment</button>
</form>
```

#### Validating Tokens

On the server side, add the `[ValidateAntiForgeryToken]` attribute to your handlers.

```csharp
[ValidateAntiForgeryToken]
public IActionResult OnPostSubmit(string comment)
{
    if (string.IsNullOrWhiteSpace(comment))
    {
        return BadRequest("Comment cannot be empty.");
    }

    return Content($"<p>{comment}</p>", "text/html");
}
```

### Authentication

If you’re building a secure area of your site, make sure your htmx requests are properly authenticated. Razor Pages with `AuthorizeAttribute` work perfectly fine with htmx.

#### Example

```csharp
[Authorize]
public class AdminModel : PageModel
{
    public IActionResult OnGetSensitiveData()
    {
        return Content("<p>Super secret information.</p>", "text/html");
    }
}
```

If the user isn’t authenticated, htmx will simply display the login page in the target element. Not ideal, but definitely secure.

## Best Practices for Secure htmx-Based Applications

To make sure you’re keeping your htmx-powered apps safe, follow these guidelines:

### 1\. Always Validate Data on the Server

Never trust the client. Use model binding, validation attributes, and manual validation as needed.

### 2\. Implement CSRF Protection

Use `@Html.AntiForgeryToken()` and the `[ValidateAntiForgeryToken]` attribute to ensure requests are coming from authenticated users.

### 3\. Use `Authorize` Attributes Where Needed

If certain actions require authentication, mark them with `[Authorize]`. htmx will gracefully handle unauthorized requests by returning the login page or an error message.

### 4\. Don’t Return Sensitive Data Without Authentication

Always ensure your backend checks user permissions before serving sensitive data. Just because a request comes from htmx doesn’t mean it’s trustworthy.

### 5\. Monitor Your Logs

Keep an eye on your server logs for suspicious requests. htmx requests will appear like any other HTTP request, so ensure you have proper logging in place.

## Conclusion

Security should never be an afterthought. By applying these best practices, you can ensure that your htmx-powered Razor Pages apps remain safe and sound. Next time, we’ll cover some performance tips to make your apps feel even more responsive.
