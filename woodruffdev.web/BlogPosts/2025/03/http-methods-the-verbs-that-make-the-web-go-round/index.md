---
title: "HTTP Methods: The Verbs That Make the Web Go Round"
date: 2025-03-11
categories: 
  - "http-rest"
tags: 
  - "http"
  - "network"
coverImage: "what_is_rest_api.png"
---

If HTTP were a language, its methods—also known as verbs—would be the action words that keep the internet running. Every time you load a webpage, submit a form, or delete a post, you're using one of these methods. Understanding them is key to working with web APIs, debugging issues, and just generally feeling like a web wizard. Let’s break them down.

* * *

## GET: Retrieving Resources

The most common and innocent of all HTTP methods, `GET` is like knocking on a server’s door and asking politely for information. It doesn’t modify anything; it just fetches data.

### Example Request:

```
GET /books HTTP/1.1
Host: example.com
```

### Example Response:

```
HTTP/1.1 200 OK
Content-Type: application/json

[
  { "id": 1, "title": "The Pragmatic Programmer" },
  { "id": 2, "title": "Clean Code" }
]
```

Since `GET` only retrieves data, it’s considered a **safe** method—no changes happen on the server. It’s also **idempotent**, meaning no matter how many times you send the request, the result is the same.

* * *

## POST: Creating or Submitting Data

When you need to create a new resource, `POST` is your go-to method. Think of it as submitting a form to sign up for a newsletter or posting a comment.

### Example Request:

```
POST /books HTTP/1.1
Host: example.com
Content-Type: application/json

{
  "title": "You Don’t Know JS"
}
```

### Example Response:

```
HTTP/1.1 201 Created
Location: /books/3
```

Unlike `GET`, `POST` is **not idempotent**—sending the same request multiple times could create multiple resources (multiple books in this case). That’s why you should be careful with duplicate submissions.

* * *

## PUT: Updating Existing Data

If `POST` is about creating, `PUT` is about updating. When you send a `PUT` request, you’re replacing an existing resource with a new version.

### Example Request:

```
PUT /books/1 HTTP/1.1
Host: example.com
Content-Type: application/json

{
  "title": "The Pragmatic Programmer (Updated Edition)"
}
```

### Example Response:

```
HTTP/1.1 200 OK
```

`PUT` is **idempotent**, meaning if you send the same request multiple times, the result is always the same. It fully replaces the resource, so if some fields are missing, they might be removed.

* * *

## DELETE: Removing Data

The `DELETE` method does exactly what it sounds like—it removes a resource from the server.

### Example Request:

```
DELETE /books/2 HTTP/1.1
Host: example.com
```

### Example Response:

```
HTTP/1.1 204 No Content
```

`DELETE` is technically **idempotent**, meaning if you delete the same resource multiple times, the result should be the same (the resource stays deleted). However, some implementations might return different responses on subsequent requests.

* * *

## Other HTTP Methods You Should Know

While `GET`, `POST`, `PUT`, and `DELETE` get most of the attention, there are a few other HTTP methods worth knowing:

- **PATCH** – Unlike `PUT`, which replaces an entire resource, `PATCH` only updates specific fields.

- **HEAD** – Just like `GET`, but it only returns headers, not the body.

- **OPTIONS** – Asks the server what methods are supported for a resource.

* * *

## Idempotency & Safety: Why They Matter

### **Safe Methods**

A method is considered **safe** if it doesn’t modify the server’s state. `GET` and `HEAD` are safe because they only retrieve data.

### **Idempotent Methods**

A method is **idempotent** if sending the same request multiple times has the same effect. `GET`, `PUT`, and `DELETE` are idempotent because calling them repeatedly doesn’t change the result. `POST`, on the other hand, is not idempotent—submitting the same form twice could create duplicate data.

* * *

## Conclusion

HTTP methods are the backbone of web communication. Whether you’re building APIs, debugging network requests, or just trying to understand how the web works, knowing how these methods behave will make you a better developer. Master these, and you’ll never look at a network request the same way again.
