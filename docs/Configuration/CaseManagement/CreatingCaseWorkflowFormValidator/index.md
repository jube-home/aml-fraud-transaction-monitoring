---
layout: default
title: Creating Case Workflow Form Validator
nav_order: 40
parent: Case Management
grand_parent: Configuration
---

# Creating Case Workflow Form Validator

A [Case Workflow Form](../CreatingCaseWorkflowForm/index.html) captures structured data on a case, and the template of the form decides only what kind of value each field takes: a number is a number, a date is a date, and a choice is one of the choices offered. The template has no means of stating that a field must be completed, that an amount must be positive, that the last transaction cannot precede the first, or that an account number must be one already known. A Form validator exists for the purposes of stating exactly that, which is to say a rule that a submission of the form must satisfy before it is accepted.

A Form validator is one rule, for one field, of one form. A form may have any number of validators, and a field may have several. The rules are evaluated in the server upon the form being submitted in the case page, hence they cannot be circumvented by the browser, and a submission failing any of them is refused in its entirety, with the reason shown beneath the field concerned.

The terms used in this page are as follows:

| Term       | Meaning                                                                                                                              |
|------------|--------------------------------------------------------------------------------------------------------------------------------------|
| Field      | An `input`, `select` or `textarea` element of the form template, identified by its `name` attribute.                                 |
| Kind       | The type of rule, of which there are nine, ranging from Required to SQL.                                                             |
| Argument   | A value qualifying the rule, such as a minimum, a pattern or the name of another field. A kind takes none, one or two arguments.     |
| Submission | The values entered into the form by the end user, sent to the server upon Submit form being clicked.                                 |

## Where it lives

Navigate through the menu to Models, open the model, then the Cases Workflows hub card, open the Case Workflow, then the Form validators hub card. The validators of every form of the Case Workflow are listed together, with the columns Name, Field, Kind and Active, and the form to which a validator belongs is chosen in the validator itself.

Creating, changing and deleting a Form validator follows the common manner described in [Child Objects](../../../Navigation/ChildObjects/index.html), with the exception that the children of a Case Workflow are not subject to approval, and a change takes effect upon being saved, for the very next submission of the form. As a validator is paired with a field of a form by name, the form must exist, with the field in its template, before the validator can be saved.

{% comment %}SHOT id=case-workflow-form-validator-record
     route=/models
     steps=open-default-model, open-hub:Cases Workflows, open-first-row, open-hub:Form validators, open-first-row
     frame=record
     state=a Form validator of the kind Compare with another field opened from the Form validators list of a Case Workflow, showing the Identity group with Form, Name, Active and Field name, and the Rule group with Kind, Argument1, Argument2 and Message
     conveys=a Form validator is one rule for one named field of one form, and the arguments shown depend on the kind of rule
     alt=A Form validator record showing the form it belongs to, its name, the field name it checks, the kind of rule, two arguments and the message shown on failure{% endcomment %}

## Fields

The form is arranged in the groups Identity, Rule and Audit, the latter being collapsed and read only. For a new validator Active is off and the Kind is Required, and it follows that a validator has no effect upon submissions until Active has been switched on and saved. The arguments presented follow the Kind as it is chosen, as tabulated under The kinds of rule.

| Value      | Description                                                                                                                                                                                                                                              | Example                                   |
|------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-------------------------------------------|
| Form       | The Case Workflow Form whose submissions the validator checks, chosen from the forms of the Case Workflow. Required. The form cannot be changed once the validator has been created.                                                                     | SAR                                       |
| Name       | The name of the validator, as shown in the list. Required and limited to 100 characters.                                                                                                                                                                 | Last is not before first                  |
| Active     | Whether the validator is run upon the form being submitted. A validator that is not active is ignored.                                                                                                                                                   | True                                      |
| Field name | The `name` attribute of the element in the form template that the validator checks. The pairing is by name, without regard to case, and the name must be that of a field in the template.                                                                | last                                      |
| Kind       | The kind of rule, as described below. Required is the default for a new validator.                                                                                                                                                                       | Compare with another field                |
| Argument1  | The first argument, its meaning depending on the Kind. It is not shown for the kinds Required and SQL (must return true).                                                                                                                                | ge                                        |
| Argument2  | The second argument, its meaning depending on the Kind. It is shown only for the kinds taking two arguments.                                                                                                                                             | first                                     |
| SQL        | Shown only for the kind SQL (must return true): a single SELECT returning one boolean. Limited to 8,000 characters.                                                                                                                                      | As below.                                 |
| Message    | Optional. The text shown to the end user where the rule fails, in place of the standard message for the kind. Limited to 500 characters. A message written here is shown as written, in every language of the user interface.                           | The last transaction precedes the first.  |

## The kinds of rule

Two of the kinds are concerned with whether a value is present, and the remaining seven with whether a value that is present is acceptable. It follows that the latter seven pass a field left empty, and a field that must both be completed and be acceptable takes two validators, the first of them Required.

| Kind                                       | Argument1                                             | Argument2                                 | Passes where                                                                                                                                                                                                                                                    | Standard message                                             |
|--------------------------------------------|-------------------------------------------------------|-------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------|
| Required                                   |                                                       |                                           | The field has a value. For a checkbox, the box is ticked.                                                                                                                                                                                                       | A value is required.                                         |
| Required when another field equals a value | The name of the other field.                          | The value of the other field.             | The field has a value, or the other field does not equal the value given. The comparison is made without regard to case.                                                                                                                                       | A value is required.                                         |
| Range (numbers or dates)                   | The minimum, blank for none.                          | The maximum, blank for none.              | The value is on or between the bounds. At least one bound is required. For a `number` field the bounds are numbers, and for a `date` or `datetime-local` field they are dates, written in the manner of `2024-01-31` or `2024-01-31T09:00`.                  | The value is outside the allowed range.                      |
| Length (characters)                        | The minimum number of characters, blank for none.     | The maximum number, blank for none.       | The number of characters in the value is on or between the bounds, each being a whole number of zero or more. At least one bound is required.                                                                                                                  | That value is longer than the field allows.                  |
| Pattern (regular expression)               | The regular expression, limited to 500 characters.    |                                           | The expression matches somewhere within the value. To insist that the whole value matches, the expression must begin with `^` and end with `$`.                                                                                                                | The value does not match the required pattern.               |
| Compare with another field                 | The relation: `eq`, `ne`, `lt`, `le`, `gt` or `ge`.   | The name of the other field.              | The value stands in the relation to the value of the other field, read as field, relation, other field. Where the other field is empty, the rule passes.                                                                                                       | The value does not agree with the field it is compared with. |
| In a model list                            | The name of a List of the model.                      |                                           | The value is an approved value of the List, without regard to case.                                                                                                                                                                                             | The value is not on the list.                                |
| In a model dictionary                      | The name of a Dictionary of the model.                |                                           | The value is an approved key of the Dictionary, without regard to case.                                                                                                                                                                                         | The value is not a known key.                                |
| SQL (must return true)                     |                                                       |                                           | The SELECT in the SQL field returns exactly one row of exactly one column, and that column is the boolean true.                                                                                                                                                 | The value did not pass the check.                            |

The standard message of Length (characters) is That value is not of a length the field allows., which is shown whether the value is shorter than the minimum or longer than the maximum, hence a validator is best given a Message of its own that states the bounds.

The relations of Compare with another field are equal (`eq`), not equal (`ne`), less than (`lt`), less than or equal (`le`), greater than (`gt`) and greater than or equal (`ge`). The two values are compared as numbers where both are numbers and the field is not a `text` field, as dates where the field is a `date` or `datetime-local` field, and otherwise character by character as text.

Certain kinds apply only to certain types of field, as a range of a checkbox would be without meaning:

| Kind                                           | Types of field                                                    |
|------------------------------------------------|-------------------------------------------------------------------|
| Range (numbers or dates)                       | `number`, `date` and `datetime-local`.                            |
| Length (characters), Pattern (regular expression) | `text`, `email`, `tel` and `textarea`.                         |
| In a model list, In a model dictionary         | `text`, `email`, `tel` and `number`.                              |
| All other kinds                                | Any field.                                                        |

### Pattern

A pattern is tested by a regular expression engine that does not backtrack, for the purposes of ensuring that no expression, however written, can occupy the server for an unbounded time. The consequence is that constructs relying on backtracking, namely backreferences and lookahead or lookbehind assertions, are not available, and a pattern using them is refused upon saving. A pattern is furthermore given 250 milliseconds to complete, after which the value is taken to have failed.

### Lists and Dictionaries

The List or Dictionary is found by name within the model to which the Case Workflow belongs, and must exist at the time the validator is saved. See [Lists](../../Models/Lists/index.html) and [Dictionaries](../../Models/Dictionaries/index.html). Only values that have been approved are considered, such that a value awaiting approval is not yet on the list for the purposes of the form.

### SQL

The SQL kind is for anything the other kinds cannot express, typically a check against data already held, such as whether an account number entered is one that has been seen. The statement is a single SELECT, and it is good practice to cast the result to boolean, as anything other than the boolean true (including no row, more than one row, more than one column, or a null) is a failure.

The statement may refer to the following bound parameters:

| Parameter       | Value                                                                                                   |
|-----------------|---------------------------------------------------------------------------------------------------------|
| `@value`        | The value of the field the validator checks, as text.                                                   |
| `@CaseId`       | The Case Id of the case on which the form is submitted, as an integer.                                  |
| `@CaseKeyValue` | The Case Key Value of that case, as text.                                                               |
| `@` field name  | The value of any other field of the form, by its name, as text. For example `@first` for a field named first. |

As every value of the form arrives as text, a value to be treated as a number or a date is cast in the statement, for example `@value::numeric`. A field of the form which has no value in the submission, or which the template does not show for the values entered, is not bound, and a statement referring to it cannot be run.

The statement is read only, sees only the rows of the tenant, and is allowed 3 seconds. A statement that fails for any reason (a timeout, an error, an unbound parameter) is a failed validation and not a pass, and is reported to the end user as The check could not be run. The text of a database error is never shown.

## How a submission is checked

Upon Submit form being clicked in the case page, the server proceeds as follows:

1. The submission is first checked against the template itself, as described in [Case Workflow Form](../CreatingCaseWorkflowForm/index.html): each value against the type of its field and its choices. Where this fails, the validators are not run.
2. The active validators of the form are then run in the order in which they were created.
3. A validator whose field is not shown by the template for the values entered (a field hidden by `v-if`) is passed over, as is a validator whose field is no longer in the template.
4. Upon a field failing a validator, the remaining validators of that same field are passed over, such that a field reports one failure at a time. The validators of the other fields continue to run.
5. Where any field has failed, nothing is stored, no Notification or HTTP Endpoint is dispatched, and the failures are returned together.

In the case page, the dialog of the form remains open with the values as entered. The message of each failure is shown in red directly beneath its field, and the field is marked as invalid. The messages are cleared upon any value of the form being changed, and return if the form is submitted again without the failure having been remedied. Where every validator passes, the submission proceeds, and confirmation is returned as Form submitted.

## Validation

Validation of the Form validator itself takes place in the server upon saving. A validator is refused, with the message shown against the field concerned, where:

| Rule                                                                                                          | Message                                                                                             |
|---------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------|
| The Form is not chosen.                                                                                       | The form is required.                                                                               |
| The Form does not exist within the tenant.                                                                    | The form was not found.                                                                             |
| The Name is empty.                                                                                            | A name is required.                                                                                 |
| The Name is longer than 100 characters.                                                                       | The name is longer than 100 characters.                                                             |
| The Field name is empty.                                                                                      | The field name is required.                                                                         |
| The Field name, or the name of another field given as an argument, is not a letter followed by up to 63 letters, digits and underscores. | The field name must start with a letter and use only letters, digits and underscores. |
| The Kind is not one of the nine.                                                                              | The kind of rule is not one of the kinds offered.                                                   |
| The Message is longer than 500 characters.                                                                    | The message is longer than 500 characters.                                                          |
| An argument that the Kind requires is empty.                                                                  | This kind of rule needs a value in Argument1. (or Argument2)                                        |
| Range or Length has neither a minimum nor a maximum.                                                          | Give a minimum, a maximum or both.                                                                  |
| A bound of Range is neither a number nor a date.                                                              | Argument1 and Argument2 must be a number.                                                           |
| A bound of Length is not a whole number of zero or more.                                                      | Argument1 and Argument2 must be a whole number of zero or more.                                     |
| The minimum exceeds the maximum.                                                                              | The minimum is greater than the maximum.                                                            |
| The pattern, or the name of the List or Dictionary, is longer than 500 characters.                            | Argument1 is longer than the limit of 500 characters.                                               |
| The pattern is not a regular expression that the engine accepts.                                              | The pattern is not a valid regular expression.                                                      |
| The relation of Compare is not one of the six.                                                                | The relation must be one of eq, ne, lt, le, gt or ge.                                               |
| The SQL is empty for the SQL kind.                                                                            | The SQL is required for this kind of rule.                                                          |
| The SQL is longer than 8,000 characters.                                                                      | The SQL is longer than 8,000 characters.                                                            |

Having passed the above, the validator is then checked against the form to which it belongs:

| Rule                                                                                                          | Message                                                                                             |
|---------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------|
| The template of the form does not compile.                                                                    | The form template does not compile: followed by the reason.                                         |
| The template has no field of the Field name.                                                                  | The form template has no field with that name.                                                      |
| The Kind does not apply to the type of the field, as tabulated above.                                         | That kind of rule does not apply to the type of that field.                                         |
| The other field named by Required when or Compare is not in the template.                                     | The form template has no field named, followed by the name.                                         |
| The model has no List of the name given.                                                                      | The model has no list with that name.                                                               |
| The model has no Dictionary of the name given.                                                                | The model has no dictionary with that name.                                                         |
| The SQL refers to a parameter that is neither built in nor a field of the form.                               | The SQL uses the parameter @name, which is neither a built-in parameter nor a field of the form.    |
| The SQL is not a single read only SELECT, or reads a table the author is not permitted to read.               | The SQL was refused: followed by the reason.                                                        |

Keep in mind that this validation takes place when the validator is saved, and not again when the form is changed. A field renamed or removed from the template afterwards leaves its validators in place but passed over, hence the validators should be revisited whenever the template of a form is altered.

## Example

Taking the SAR form of [Case Workflow Form](../CreatingCaseWorkflowForm/index.html), which has the fields `name`, `DOB`, `first`, `last`, `address` and `comments`, the following four validators ensure that the name is given, that the date of birth is plausible, that the last transaction does not precede the first, and that comments are not excessive:

| Name                      | Field name | Kind                         | Argument1    | Argument2    | Message                                  |
|---------------------------|------------|------------------------------|--------------|--------------|------------------------------------------|
| Name is given             | name       | Required                     |              |              |                                          |
| Date of birth is sensible | DOB        | Range (numbers or dates)     | 1900-01-01   |              | The date of birth is before 1900.        |
| Last is not before first  | last       | Compare with another field   | ge           | first        | The last transaction precedes the first. |
| Comments are bounded      | comments   | Length (characters)          |              | 2000         |                                          |

Submitting the form with the Name left empty, a First Transaction of 10 March and a Last Transaction of 3 March is refused, with A value is required. beneath Name and The last transaction precedes the first. beneath Last Transaction. Nothing is recorded in the Forms section of the case until both are remedied. Leaving DOB empty is accepted, as a Range passes an empty value.

A validator of the SQL kind, on a form having a field named `account`, which accepts only an account for which the case management has previously raised a case, might be written as follows:

```sql
SELECT EXISTS (
  SELECT 1
  FROM "Case"
  WHERE "CaseKeyValue" = @value
)::boolean
```

## Testing a validator

A saved validator can be tried against values without submitting a form, and without anything being stored. The facility is not presented in the user interface: it is available through the API as `POST /api/CaseWorkflowFormValidator/Test`, and to Ask Jooby as the operation CaseWorkflowFormValidatorTest.

```http
POST /api/CaseWorkflowFormValidator/Test
Content-Type: application/json

{
  "id": 12,
  "caseId": 1042,
  "values": {
    "first": "2024-03-10T09:00",
    "last": "2024-03-03T09:00"
  }
}
```

| Value  | Description                                                                                                                                                      |
|--------|------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| id     | The identifier of the validator to try. The validator is tried whether or not it is Active.                                                                      |
| caseId | A case supplying `@CaseId` and `@CaseKeyValue` to a SQL validator, or 0 for none, in which circumstance `@CaseId` is 0 and `@CaseKeyValue` is empty.              |
| values | The values to try, by field name. The value of the field that the validator checks is the value tested, and the remainder are available to the rule as the other fields. |

The response states whether the value passed, and where it did not, why:

```json
{
  "passed": false,
  "code": "compare",
  "message": "The last transaction precedes the first.",
  "milliseconds": 1
}
```

| Value        | Description                                                                                                                                                         |
|--------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| passed       | True where the value satisfied the rule.                                                                                                                            |
| code         | Where it failed, one of `required`, `range`, `length`, `pattern`, `compare`, `list`, `dictionary`, `sql`, or `sqlFailed` where the SQL could not be run.            |
| message      | Where it failed, the Message of the validator, or the standard message of the kind in the language of the request.                                                  |
| milliseconds | The time taken by the check, which for a SQL validator is the means of observing how close the statement is to the limit of 3 seconds.                              |

Only the one validator is tried. The template checks, and the other validators of the form, take no part. Where the template of the form does not compile, the request is refused as a validation failure stating as much, and where the validator does not exist, nothing is returned.

{% comment %}TODO(verify): where the field of the validator is no longer in the template, Test returns passed false with no code, zero milliseconds and the message The value did not pass the check. (FormValidatorEngine.TestAsync with FormValidatorMessages.For), which misleads. Owner to decide whether a distinct code and message are wanted.{% endcomment %}

{% comment %}TODO(verify): the Form validators record has no control to test a validator, the facility being API and Ask Jooby only. Owner to confirm whether a Test control is to be added to the record, at which point this section should describe it.{% endcomment %}

## Permissions and security

Listing, creating, changing, deleting and testing a Form validator requires permission 21, Read Write Cases Workflow Form, being the same permission as the form itself. Creating, changing, deleting or testing a validator of the SQL kind, and changing a validator to or from the SQL kind, requires in addition permission 67, Author Case Workflow SQL, in the absence of which the request is refused with You do not have permission to author SQL validators. A request lacking the permission is refused with the HTTP status 403, a validator failing validation with 400, and a change made upon a version of the validator that has since been changed by another user with 409.

The tables that a SQL validator may read are checked against the permissions of its author at the time of saving. Thereafter the statement is run for every user who submits the form, whatever the table permissions of that user, and it follows that permission 67 should be granted only to those who would be trusted to write such a statement. The risk is narrower than that of the SQL of a Display or a Form, as a validator returns nothing but pass or fail to the end user. The statement is read only, is confined to the rows of the tenant, and receives the values of the submission as bound parameters only, such that a value entered into a form cannot alter the statement.

Submitting a form, and thereby running its validators, requires permission 1, Read Write Case, as described in [Case Workflow Form](../CreatingCaseWorkflowForm/index.html). A validator belongs to the tenant of its Case Workflow and is neither visible to, nor run for, any other tenant.
