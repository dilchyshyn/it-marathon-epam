import { DishListResponse } from '@app/models/dishes/dish-list-response.model';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DishSelectCardComponent } from './dish-select-card.component';

describe('DishSelectCardComponent', () => {
  let component: DishSelectCardComponent;
  let fixture: ComponentFixture<DishSelectCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DishSelectCardComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(DishSelectCardComponent);
    component = fixture.componentInstance;
    component.dish = { id: 'dish-1', name: 'Tomato Soup', price: 8, weight: '300g', imageUrl: 'assets/img/dish.png', description: 'Soup' } as unknown as DishListResponse;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
