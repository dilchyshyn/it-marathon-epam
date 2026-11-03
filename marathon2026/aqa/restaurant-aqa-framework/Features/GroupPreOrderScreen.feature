@ui @group-preorder
Feature: Group pre-order journeys

  What the Owner and the waiter actually see in the browser. Guests and their carts are prepared
  through the service; every check below is made on the screen.

  Background:
    Given a restaurant is open with this menu:
      | dish  | price |
      | Salad | 10    |
      | Soup  | 8     |
      | Steak | 25    |
    And a customer owns a group reservation for 6 people

  Rule: The Owner sees every participant, their dishes and the totals

    @level1 @smoke
    Scenario: The Owner sees the participants and the totals
      Given these guests have joined:
        | guest  | dish  | quantity | cart      |
        | Anna   | Salad | 2        | submitted |
        | Anna   | Soup  | 1        | submitted |
        | Bohdan | Steak | 1        | submitted |
      When the owner opens the group order
      Then the group shows these guests:
        | guest  | dishes              | total | cart      |
        | Anna   | Salad x 2, Soup x 1 | 28    | submitted |
        | Bohdan | Steak x 1           | 25    | submitted |
      And the group total is 53

    @level1
    Scenario: A changing cart updates the guest's sum, not yet the Grand Total
      Given these guests have joined:
        | guest | dish  | quantity | cart           |
        | Anna  | Salad | 1        | still choosing |
      When the owner opens the group order
      And "Anna" adds 2 more of "Soup" to their cart
      And the owner refreshes the group order
      Then the group shows these guests:
        | guest | dishes              | total | cart           |
        | Anna  | Salad x 1, Soup x 2 | 26    | still choosing |
      And the group total is 0

    @level1
    Scenario: Guests still choosing, and guests with no dishes, are marked
      Given these guests have joined:
        | guest  | dish  | quantity | cart           |
        | Anna   | Salad | 1        | still choosing |
        | Bohdan |       |          | still choosing |
      When the owner opens the group order
      Then the group shows these guests:
        | guest  | dishes    | total | cart           |
        | Anna   | Salad x 1 | 10    | still choosing |
        | Bohdan | none      | 0     | still choosing |
      And the owner is warned that 2 carts have not been submitted

    # Known defect (expected to fail until fixed): opening the screen creates the Owner's own session, and the next
    # refresh counts it, so the warning names one person more than the guests who are still choosing.
    @level1 @known-defect
    Scenario: The warning keeps counting only the guests after a refresh
      Given these guests have joined:
        | guest  | dish  | quantity | cart           |
        | Anna   | Salad | 1        | still choosing |
        | Bohdan | Soup  | 1        | still choosing |
      When the owner opens the group order
      And the owner refreshes the group order
      Then the owner is warned that 2 carts have not been submitted

    @level1
    Scenario: A group nobody has joined yet
      When the owner opens the group order
      Then the owner is told nobody has chosen any dishes yet

  Rule: The Owner sends one consolidated pre-order

    @level2 @smoke
    Scenario: Same dishes are merged and carts still being chosen are left out
      Given these guests have joined:
        | guest  | dish  | quantity | cart           |
        | Anna   | Salad | 2        | submitted      |
        | Bohdan | Salad | 1        | submitted      |
        | Bohdan | Soup  | 1        | submitted      |
        | Olena  | Steak | 3        | still choosing |
      And the owner has started their own pre-order with "Steak"
      When the owner opens the group order
      Then the consolidated pre-order holds "Salad x 3, Soup x 1"
      When the owner submits the pre-order on the screen
      Then the owner sees that the pre-order was sent to the restaurant
      And the group is marked as submitted

    @level2
    Scenario: The waiter sees the order the Owner confirmed
      Given these guests have joined:
        | guest | dish  | quantity | cart      |
        | Anna  | Salad | 2        | submitted |
      And the owner has started their own pre-order with "Steak"
      When the owner opens the group order
      And the owner submits the pre-order on the screen
      Then the owner sees that the pre-order was sent to the restaurant
      When the waiter opens the owner's pre-order
      Then the waiter sees "Salad x 2, Steak x 1"

    @level2 @negative
    Scenario: The Owner is told when there is no pre-order to add to
      Given these guests have joined:
        | guest | dish  | quantity | cart      |
        | Anna  | Salad | 1        | submitted |
      When the owner opens the group order
      And the owner submits the pre-order on the screen
      Then the owner is told the pre-order could not be sent

  Rule: The Owner can remove a participant from the group

    @level3
    Scenario: Removing a guest updates the list and the total
      Given these guests have joined:
        | guest  | dish  | quantity | cart      |
        | Anna   | Salad | 2        | submitted |
        | Bohdan | Soup  | 1        | submitted |
      When the owner opens the group order
      And the owner removes "Bohdan" on the screen
      Then the group shows these guests:
        | guest | dishes    | total | cart      |
        | Anna  | Salad x 2 | 20    | submitted |
      And the group total is 20

    @level3 @smoke
    Scenario: From choosing dishes to the waiter, without the removed guest
      Given these guests have joined:
        | guest  | dish  | quantity | cart      |
        | Anna   | Salad | 1        | submitted |
        | Bohdan | Soup  | 2        | submitted |
        | Olena  | Salad | 1        | submitted |
      And the owner has started their own pre-order with "Steak"
      When the owner opens the group order
      And the owner removes "Bohdan" on the screen
      Then the consolidated pre-order holds "Salad x 2"
      When the owner submits the pre-order on the screen
      Then the owner sees that the pre-order was sent to the restaurant
      When the waiter opens the owner's pre-order
      Then the waiter sees "Salad x 2, Steak x 1"
