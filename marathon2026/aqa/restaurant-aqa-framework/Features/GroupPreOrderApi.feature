@api @group-preorder
Feature: Group pre-order rules

  An Owner invites guests to a reservation, each guest builds a cart, and the Owner sends one
  consolidated pre-order to the restaurant. Who may do what, and what happens when something
  is wrong, is checked here; the main journeys are in the screen feature.

  Background:
    Given a restaurant is open with this menu:
      | dish  | price |
      | Salad | 10    |
      | Soup  | 8     |
      | Steak | 25    |
    And a customer owns a group reservation for 6 people

  Rule: The Owner sees every participant, their dishes and the totals

    @level1 @smoke
    Scenario: The Owner sees what each guest chose and the totals
      Given these guests have joined:
        | guest  | dish  | quantity | cart      |
        | Anna   | Salad | 2        | submitted |
        | Anna   | Soup  | 1        | submitted |
        | Bohdan | Steak | 1        | submitted |
      When the owner asks to see the group order
      Then the group shows these guests:
        | guest  | dishes             | total | cart      |
        | Anna   | Salad x 2, Soup x 1 | 28    | submitted |
        | Bohdan | Steak x 1          | 25    | submitted |
      And the group total is 53

    @level1
    Scenario: The total follows a guest's changing cart
      Given these guests have joined:
        | guest | dish  | quantity | cart           |
        | Anna  | Salad | 1        | still choosing |
      When "Anna" adds 2 more of "Soup" to their cart
      And the owner asks to see the group order
      Then the group total is 26

    @level1
    Scenario: A guest who has not submitted, and one with no dishes, are both listed
      Given these guests have joined:
        | guest  | dish  | quantity | cart           |
        | Anna   | Salad | 1        | still choosing |
        | Bohdan |       |          | still choosing |
      When the owner asks to see the group order
      Then the group shows these guests:
        | guest  | dishes    | total | cart           |
        | Anna   | Salad x 1 | 10    | still choosing |
        | Bohdan | none      | 0     | still choosing |

    @level1
    Scenario: A group nobody has joined yet is empty
      When the owner asks to see the group order
      Then the group has no participants
      And the group total is 0

    @level1 @negative
    Scenario Outline: Only the Owner sees the group
      Given these guests have joined:
        | guest | dish  | quantity | cart      |
        | Anna  | Salad | 1        | submitted |
      When <who> asks to see the group order
      Then the request is refused because <reason>

      Examples:
        | who                  | reason                    |
        | another customer     | only the owner may do this |
        | the waiter           | only the owner may do this |
        | a guest              | the caller is not signed in |

    @level1 @negative
    Scenario: A visitor who is not signed in does not see the group
      When an anonymous visitor asks to see the group order
      Then the request is refused because the caller is not signed in

    @level1 @negative
    Scenario: A group that does not exist
      When the owner asks to see a group that does not exist
      Then the group cannot be found

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
      When the owner submits the group pre-order
      Then the restaurant is sent "Salad x 3, Soup x 1"
      And the group is marked as submitted
      And the waiter's order is submitted and holds "Salad x 3, Soup x 1, Steak x 1"

    @level2 @negative
    Scenario: Nothing can be sent while no cart has been submitted
      Given these guests have joined:
        | guest | dish  | quantity | cart           |
        | Anna  | Salad | 1        | still choosing |
      And the owner has started their own pre-order with "Steak"
      When the owner submits the group pre-order
      Then the request is rejected with the explanation "No submitted guest sessions"

    # The backend refuses to submit an empty cart, so "submitted carts without dishes" cannot
    # arise through the product. The rule is enforced one step earlier, which is what is tested.
    @level2 @negative
    Scenario: A cart without dishes cannot be submitted
      Given these guests have joined:
        | guest | dish | quantity | cart           |
        | Anna  |      |          | still choosing |
      When "Anna" tries to submit their empty cart
      Then the request is rejected with the explanation "The cart is empty"

    @level2 @negative
    Scenario: Nothing can be sent when the Owner has no pre-order to add to
      Given these guests have joined:
        | guest | dish  | quantity | cart      |
        | Anna  | Salad | 1        | submitted |
      When the owner submits the group pre-order
      Then the request is rejected with the explanation "not found"

    @level2 @negative
    Scenario: The group does not exist
      When the owner submits a group that does not exist
      Then the group cannot be found

    @level2 @negative
    Scenario: The reservation does not exist
      When the owner submits the group for a reservation that does not exist
      Then the reservation cannot be found

    @level2 @negative
    Scenario: A group cannot be submitted twice
      Given these guests have joined:
        | guest | dish  | quantity | cart      |
        | Anna  | Salad | 1        | submitted |
      And the owner has started their own pre-order with "Steak"
      When the owner submits the group pre-order
      And the owner submits the group pre-order
      Then the request is rejected with the explanation "was submitted"

    @level2 @negative
    Scenario Outline: Only the Owner can submit
      Given these guests have joined:
        | guest | dish  | quantity | cart      |
        | Anna  | Salad | 1        | submitted |
      And the owner has started their own pre-order with "Steak"
      When <who> submits the group pre-order
      Then the request is refused because <reason>

      Examples:
        | who              | reason                     |
        | another customer | only the owner may do this |
        | the waiter       | only the owner may do this |
        | a guest          | the caller is not signed in |

    @level2 @negative
    Scenario: A visitor who is not signed in cannot submit
      When an anonymous visitor submits the group pre-order
      Then the request is refused because the caller is not signed in

  Rule: The Owner can remove a participant from the group

    Background:
      Given these guests have joined:
        | guest  | dish  | quantity | cart           |
        | Anna   | Salad | 2        | submitted      |
        | Bohdan | Soup  | 1        | still choosing |

    @level3 @smoke
    Scenario: The rest stay and the total is recalculated
      When the owner removes "Bohdan" from the group
      Then the group shows these guests:
        | guest | dishes    | total | cart      |
        | Anna  | Salad x 2 | 20    | submitted |
      And the group total is 20

    @level3
    Scenario: A guest who already submitted can be removed
      When the owner removes "Anna" from the group
      Then the group shows these guests:
        | guest  | dishes   | total | cart           |
        | Bohdan | Soup x 1 | 8     | still choosing |
      And the group total is 8

    @level3
    Scenario: The last guest can be removed
      When the owner removes "Anna" from the group
      And the owner removes "Bohdan" from the group
      Then the group has no participants
      And the group total is 0

    @level3 @negative
    Scenario: A guest of another group cannot be removed
      Given the owner also has a second group
      And "Foreign" joined the second group
      When the owner removes "Foreign" from the group
      Then the request is rejected with the explanation "does not belong"

    @level3 @negative
    Scenario: A guest who does not exist
      When the owner removes a guest who does not exist
      Then the guest cannot be found

    @level3 @negative
    Scenario Outline: Only the Owner can remove a guest
      When <who> removes "Bohdan" from the group
      Then the request is refused because <reason>

      Examples:
        | who              | reason                     |
        | another customer | only the owner may do this |
        | the waiter       | only the owner may do this |
        | a guest          | the caller is not signed in |

    @level3 @negative
    Scenario: A visitor who is not signed in cannot remove a guest
      When an anonymous visitor removes "Bohdan" from the group
      Then the request is refused because the caller is not signed in
