import { DishListResponse } from '@app/models/dishes/dish-list-response.model';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DishModalComponent } from './dish-modal.component';

describe('DishModalComponent', () => {
  let component: DishModalComponent;
  let fixture: ComponentFixture<DishModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DishModalComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(DishModalComponent);
    component = fixture.componentInstance;
    component.dish = { id: 'dish-1', name: 'Tomato Soup', price: 8, weight: '300g', imageUrl: 'assets/img/dish.png', description: 'Soup' } as unknown as DishListResponse;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
